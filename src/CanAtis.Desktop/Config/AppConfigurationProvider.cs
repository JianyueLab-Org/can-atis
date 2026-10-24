// <copyright file="AppConfigurationProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using CanAtis.Events;
using CanAtis.Events.EventBus;
using CanAtis.Io;

namespace CanAtis.Config;

/// <inheritdoc />
public class AppConfigurationProvider : IAppConfigurationProvider
{
    private const string CanFsdHost = "fsd.ceruleanavi.net";
    private const int CanFsdPort = 6809;
    private const string CanAudioHost = "audio.ceruleanavi.net";
    private const int CanAudioPort = 64738;
    private const string CanDatafeedUrl = "https://data.ceruleanavi.net/v1/data.json";
    private readonly IDownloader _downloader;
    private readonly List<string> _metarUrls;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppConfigurationProvider"/> class.
    /// </summary>
    /// <param name="downloader">An instance of <see cref="IDownloader"/> to handle download operations.</param>
    public AppConfigurationProvider(IDownloader downloader)
    {
        _downloader = downloader;
        _metarUrls = [];
    }

    /// <summary>
    /// Gets the URL of the client update endpoint. The URL expects query parameters: client=atis-for-can&version={current_version}
    /// and returns JSON with update availability information. The <c>VATIS_VERSION_URL</c> environment variable overrides the
    /// configured value, which allows testing the update process against a local server.
    /// </summary>
    public string VersionUrl => "https://ceruleanavi.net/api/v1/clients/check-update";

    /// <inheritdoc />
    public string MetarUrl => "https://data.ceruleanavi.net/v1/data.json";

    /// <inheritdoc />
    public string NavDataUrl => "";

    /// <inheritdoc />
    public string AtisHubUrl => "";

    /// <inheritdoc />
    public string VoiceListUrl => "";

    /// <inheritdoc />
    public string TextToSpeechUrl => "";

    /// <inheritdoc />
    public string DigitalAtisApiUrl => "https://ceruleanavi.net/api/v1/atis";

    public string FsdHost => CanFsdHost;
    public int FsdPort => CanFsdPort;
    public string AudioHost => CanAudioHost;
    public int AudioPort => CanAudioPort;

    /// <inheritdoc />
    public async Task Initialize()
    {
        Log.Information("Using Cerulean Aviation Network service endpoints: FSD {FsdHost}:{FsdPort}, audio {AudioHost}:{AudioPort}",
            CanFsdHost, CanFsdPort, CanAudioHost, CanAudioPort);
        EventBus.Instance.Publish(new StartupStatusChanged("CAN 服务配置已加载"));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Downloads a startup resource, retrying transient network failures. Each attempt is given a short deadline so a
    /// stalled connection (e.g. a DNS or connect hang on a flaky network) is abandoned and retried on a fresh
    /// connection instead of blocking startup for the full HTTP timeout.
    /// </summary>
}
