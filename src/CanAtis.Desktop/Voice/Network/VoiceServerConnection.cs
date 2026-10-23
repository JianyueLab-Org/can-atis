// <copyright file="VoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
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
    private string _connectionDescription = "未连接";
    private long _lastStatus;

    /// <inheritdoc />
    public string ConnectionDescription => _connectionDescription.StartsWith("已连接", StringComparison.Ordinal) &&
        System.Diagnostics.Stopwatch.GetElapsedTime(System.Threading.Interlocked.Read(ref _lastStatus)).TotalSeconds > 5
        ? "连接中断（状态超时）" : _connectionDescription;

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
        var worker = BundledVoiceWorker.GetPath() ?? DevelopmentWorkerPath();
        if (worker == null || !File.Exists(worker))
            throw new AtisBuilderException(
                "CAN Mumble worker is missing. Release builds embed it; from source, set CAN_VOICE_WORKER " +
                "to can-audio's atis-worker/can_mumble_worker.py or a built can-voice-worker.exe.");
        var isScript = worker.EndsWith(".py", StringComparison.OrdinalIgnoreCase);

        var frequency = dto.Transceivers?.FirstOrDefault()?.Frequency ?? 0;
        var cid = _appConfig.UserId.Trim();
        var password = _appConfig.PasswordDecrypted;
        var psi = new ProcessStartInfo
        {
            FileName = isScript ? PythonPath() : worker,
            WorkingDirectory = Path.GetDirectoryName(worker) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (isScript)
            psi.ArgumentList.Add(worker);
        psi.ArgumentList.Add("--host"); psi.ArgumentList.Add("audio.ceruleanavi.net");
        psi.ArgumentList.Add("--port"); psi.ArgumentList.Add("64738");
        psi.ArgumentList.Add("--cid"); psi.ArgumentList.Add(cid);
        psi.Environment["CAN_VOICE_PASSWORD"] = password;
        psi.ArgumentList.Add("--callsign"); psi.ArgumentList.Add(callsign);

        // Hz (127800000); the worker also accepts MHz and maps either to FREQ_127800.
        psi.ArgumentList.Add("--frequency"); psi.ArgumentList.Add(frequency.ToString(CultureInfo.InvariantCulture));
        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _mumbleWorker = process;
        _connectionDescription = "连接中";
        process.OutputDataReceived += (_, e) =>
        {
            if (!ReferenceEquals(_mumbleWorker, process) || string.IsNullOrWhiteSpace(e.Data))
                return;
            try
            {
                using var json = JsonDocument.Parse(e.Data);
                var root = json.RootElement;
                var state = root.GetProperty("state").GetString();
                var description = state switch
                {
                    "connected" => $"已连接 · {root.GetProperty("transport").GetString()} · " +
                        (root.GetProperty("latency").ValueKind == JsonValueKind.Number
                            ? $"{root.GetProperty("latency").GetDouble():F0} ms" : "-- ms"),
                    "connecting" => "连接中",
                    "error" => "连接失败 · " + root.GetProperty("message").GetString(),
                    _ => "未连接"
                };
                System.Threading.Interlocked.Exchange(ref _lastStatus, System.Diagnostics.Stopwatch.GetTimestamp());
                if (_connectionDescription != description)
                    Log.Information("Voice {Callsign}: {Status}", callsign, description);
                _connectionDescription = description;
            }
            catch (JsonException)
            {
                Log.Debug("Voice worker: {Output}", e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Log.Warning("Voice worker {Callsign}: {Error}", callsign, e.Data.Replace(password, "[redacted]"));
        };
        process.Exited += (_, _) =>
        {
            if (ReferenceEquals(_mumbleWorker, process))
            {
                _connectionDescription = "未连接（语音进程已退出）";
                Log.Warning("Voice worker exited: {Callsign}, code {ExitCode}", callsign, process.ExitCode);
            }
        };
        if (!process.Start())
            throw new AtisBuilderException("Could not start the CAN Mumble worker.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await process.StandardInput.BaseStream.WriteAsync(dto.PcmData, cancellationToken);
            await process.StandardInput.BaseStream.FlushAsync(cancellationToken);
            process.StandardInput.Close();
        }
        catch
        {
            if (ReferenceEquals(_mumbleWorker, process))
                StopMumbleWorker();
            throw;
        }
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
        var process = _mumbleWorker;
        _mumbleWorker = null;
        _connectionDescription = "未连接";
        if (process is null)
            return;
        try
        {
            if (!process.HasExited)
                process.Kill(true);
            process.Dispose();
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

    // The worker is maintained in JianyueLab-Org/can-audio (atis-worker/); this repository no longer carries a copy.
    private static string? DevelopmentWorkerPath()
    {
        var path = Environment.GetEnvironmentVariable("CAN_VOICE_WORKER");
        return string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
    }

    private static string PythonPath()
    {
        var python = Environment.GetEnvironmentVariable("CAN_PYTHON");
        return string.IsNullOrWhiteSpace(python) ? "python" : python;
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
