// <copyright file="VoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Network;

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
        // CAN 的语音端是 Mumble，机器人由根目录 server/ATIS/mumble.py 管理，
        // 没有 vATIS 的 HTTP bot API。桌面端的 FSD 连接负责公开席位，
        // 这里不再向 VATSIM voice REST 端点发送请求。
        await Task.CompletedTask;
        Log.Information("CAN Mumble audio is managed by the CAN ATIS service: {Callsign}", callsign);
    }

    /// <inheritdoc />
    public async Task RemoveBot(string? callsign, CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        Log.Information("CAN Mumble audio cleanup is managed by the CAN ATIS service: {Callsign}", callsign);
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
