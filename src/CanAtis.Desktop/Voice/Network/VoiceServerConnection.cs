// <copyright file="VoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using AsyncAwaitBestPractices;
using Serilog;
using CanAtis.Atis;
using CanAtis.Config;
using CanAtis.Io;
using CanAtis.Voice.Dto;

namespace CanAtis.Voice.Network;

/// <summary>
/// Provides functionality for connecting to and interacting with a voice server.
/// </summary>
public class VoiceServerConnection : IVoiceServerConnection
{
    private const string ClientName = "Cerulean Aviation Network ATIS";
    private const string VoiceServerUrl = "https://audio.ceruleanavi.net";
    private static readonly TimeSpan s_tokenRefreshInterval = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan s_heartbeatInterval = TimeSpan.FromSeconds(30);

    private readonly IDownloader _downloader;
    private readonly IAppConfig _appConfig;
    private Timer? _refreshTokenTimer;
    private Timer? _heartbeatTimer;
    private string? _jwtToken;
    private Process? _mumbleWorker;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceServerConnection"/> class.
    /// </summary>
    /// <param name="downloader">The downloader instance used for handling download operations.</param>
    /// <param name="appConfig">The application config instance.</param>
    public VoiceServerConnection(IDownloader downloader, IAppConfig appConfig)
    {
        _downloader = downloader;
        _appConfig = appConfig;
    }

    /// <inheritdoc />
    public async Task Connect()
    {
        await Authenticate();

        // Start the timer to refresh the JWT token periodically
        _refreshTokenTimer = new Timer(RefreshTokenCallback, null, s_tokenRefreshInterval, s_tokenRefreshInterval);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        StopMumbleWorker();
        _jwtToken = null;

        // Stop token refresh timer
        _refreshTokenTimer?.Dispose();
        _refreshTokenTimer = null;

        // Stop heartbeat timer
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateBot(string? callsign, PutBotRequestDto dto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(callsign) || dto.PcmData is null || dto.PcmData.Length == 0)
            return;

        cancellationToken.ThrowIfCancellationRequested();
        StopMumbleWorker();
        var worker = WorkerPath();
        if (!File.Exists(worker))
            throw new AtisBuilderException($"CAN Mumble worker is missing: {worker}");

        var frequency = dto.Transceivers?.FirstOrDefault()?.Frequency ?? 0;
        var cid = _appConfig.UserId.Trim();
        var password = _appConfig.PasswordDecrypted;
        var psi = new ProcessStartInfo
        {
            FileName = PythonPath(),
            WorkingDirectory = Path.GetDirectoryName(worker) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(worker);
        psi.ArgumentList.Add("--host"); psi.ArgumentList.Add("audio.ceruleanavi.net");
        psi.ArgumentList.Add("--port"); psi.ArgumentList.Add("64738");
        psi.ArgumentList.Add("--cid"); psi.ArgumentList.Add(cid);
        psi.ArgumentList.Add("--password"); psi.ArgumentList.Add(password);
        psi.ArgumentList.Add("--callsign"); psi.ArgumentList.Add(callsign);
        psi.ArgumentList.Add("--frequency"); psi.ArgumentList.Add(frequency.ToString());
        _mumbleWorker = Process.Start(psi);
        if (_mumbleWorker is null)
            throw new AtisBuilderException("Could not start the CAN Mumble worker.");
        await _mumbleWorker.StandardInput.BaseStream.WriteAsync(dto.PcmData, cancellationToken);
        await _mumbleWorker.StandardInput.BaseStream.FlushAsync(cancellationToken);
        _mumbleWorker.StandardInput.Close();
        Log.Information("started CAN Mumble worker for {Callsign} on {Frequency}", callsign, frequency);
    }

    /// <inheritdoc />
    public async Task RemoveBot(string? callsign, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();
        StopMumbleWorker();
        await Task.CompletedTask;
    }

    private void HeartbeatTimerCallback(object? state)
    {
        if (state is string callsign)
        {
            SendHeartbeatAsync(callsign).SafeFireAndForget(onException: exception =>
            {
                Log.Error(exception, "SendHeartbeatAsync failed");
            });
        }
    }

    private async Task SendHeartbeatAsync(string callsign)
    {
        if (_jwtToken == null)
            await Authenticate();

        if (_jwtToken == null)
            return;

        try
        {
            var response = await _downloader.GetAsync($"{VoiceServerUrl}/api/v1/bots/{callsign}/heartbeat", _jwtToken);
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Heartbeat: {Callsign}", callsign);
            }
            else
            {
                Log.Warning("Heartbeat failed for {Callsign} – {StatusCode}", callsign, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SendHeartbeatAsync failed for callsign {Callsign}", callsign);
        }
    }

    private async Task Authenticate()
    {
        if (string.IsNullOrEmpty(_appConfig.UserId) || string.IsNullOrEmpty(_appConfig.PasswordDecrypted))
        {
            throw new AtisBuilderException("Voice server authentication failed: UserID or Password are null.");
        }

        var dto = JsonSerializer.Serialize(
            new PostUserRequestDto(_appConfig.UserId, _appConfig.PasswordDecrypted, ClientName),
            SourceGenerationContext.NewDefault.PostUserRequestDto);
        // CAN 的语音服务是 Mumble，不提供 vATIS 的 JWT/HTTP bot API。
        // FSD 登录与 Mumble 登录均由 can-audio/server 处理；保留一个成功标志，
        // 让旧的 ViewModel 生命周期继续工作，真正的语音连接由客户端广播器负责。
        _jwtToken = "CAN_MUMBLE";
    }

    private void StopMumbleWorker()
    {
        if (_mumbleWorker is null)
            return;
        try
        {
            if (!_mumbleWorker.HasExited)
                _mumbleWorker.Kill(true);
            _mumbleWorker.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "could not stop CAN Mumble worker");
        }
        finally
        {
            _mumbleWorker = null;
        }
    }

    private static string WorkerPath() => Path.Combine(AppContext.BaseDirectory, "can_mumble_worker.py");

    private static string PythonPath()
    {
        var projectVenv = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".venv", "Scripts", "python.exe"));
        if (File.Exists(projectVenv))
            return projectVenv;
        var bundled = Path.Combine(AppContext.BaseDirectory, "python", "python.exe");
        return File.Exists(bundled) ? bundled : "python";
    }

    private async Task RefreshToken()
    {
        if (string.IsNullOrEmpty(_appConfig.UserId) || string.IsNullOrEmpty(_appConfig.PasswordDecrypted))
        {
            throw new AtisBuilderException("Cannot refresh token: UserID or Password are not set.");
        }

        await Authenticate();
    }

    private void RefreshTokenCallback(object? state)
    {
        try
        {
            RefreshToken().SafeFireAndForget(onException: exception =>
            {
                Log.Error(exception, "RefreshToken Failed");
            });
            Log.Information("Voice server auth token refreshed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh voice server auth token.");
        }
    }
}
