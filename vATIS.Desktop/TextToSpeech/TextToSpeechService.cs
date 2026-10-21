// <copyright file="TextToSpeechService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.TextToSpeech;

/// <inheritdoc />
public class TextToSpeechService : ITextToSpeechService
{
    private readonly IDownloader _downloader;
    private readonly IAppConfigurationProvider _appConfigurationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextToSpeechService"/> class.
    /// </summary>
    /// <param name="downloader">The application downloader.</param>
    /// <param name="authTokenManager">The CAN authentication manager; its token is not a speech service JWT.</param>
    /// <param name="appConfigurationProvider">The application configuration provider.</param>
    public TextToSpeechService(IDownloader downloader, IAuthTokenManager authTokenManager,
        IAppConfigurationProvider appConfigurationProvider)
    {
        _downloader = downloader;
        _appConfigurationProvider = appConfigurationProvider;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> BuiltInContractions { get; private set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public List<VoiceMetaData> VoiceList { get; private set; } = [];

    /// <inheritdoc />
    public async Task Initialize()
    {
        await Task.WhenAll(LoadVoiceList(), LoadBuiltInContractions());
    }

    /// <inheritdoc />
    public async Task<byte[]?> RequestAudio(string text, AtisStation station, char atisLetter,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var voiceName = station.AtisVoice.Voice;
        if (station.AtisVoice.AlternatingVoices is { Length: >= 2 } voices)
        {
            var index = (atisLetter - station.CodeRange.Low) % voices.Length;
            voiceName = voices[index < 0 ? index + voices.Length : index];
        }

        var voiceId = VoiceList.FirstOrDefault(v => v.Name == voiceName || v.Id == voiceName)?.Id;
        if (voiceId == null && !string.IsNullOrWhiteSpace(voiceName) && voiceName != "Default")
            throw new AtisBuilderException($"TTS voice '{voiceName}' is unavailable. Reload the application and select a voice from the speech service catalogue.");

        // CAN_AUTHENTICATED is a local marker, not a JWT accepted by the vATIS speech service.
        var jwt = Environment.GetEnvironmentVariable("CAN_TTS_JWT");
        if (string.IsNullOrWhiteSpace(jwt))
            throw new AtisBuilderException("The speech service requires authentication. A shared CAN speech service must be configured before TTS is available to all CAN users.");

        var dto = new TextToSpeechRequestDto
        {
            Text = text,
            Voice = voiceId ?? "default",
            SpeechRate = station.AtisVoice.SpeechRate,
            Jwt = jwt
        };
        var json = JsonSerializer.Serialize(dto, SourceGenerationContext.NewDefault.TextToSpeechRequestDto);
        using var response = await _downloader.PostJsonResponse(_appConfigurationProvider.TextToSpeechUrl, json,
            cancellationToken: cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new AtisBuilderException("The speech service rejected the TTS token. Supply a valid, unexpired token accepted by that service.");
        if (!response.IsSuccessStatusCode)
            throw new AtisBuilderException($"The speech service could not synthesize audio (HTTP {(int)response.StatusCode}).");

        var audio = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (audio.Length == 0 || audio.Length % 2 != 0 ||
            response.Content.Headers.ContentType?.MediaType is "application/json" or "application/problem+json" or "text/html")
            throw new AtisBuilderException("The speech service did not return valid PCM audio.");
        return audio;
    }

    private async Task LoadVoiceList()
    {
        try
        {
            var response = await _downloader.DownloadStringAsync(_appConfigurationProvider.VoiceListUrl);
            var voices = JsonSerializer.Deserialize(response, SourceGenerationContext.NewDefault.ListVoiceMetaData);
            if (voices is { Count: > 0 })
                VoiceList = voices;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading vATIS speech voice list");
        }
    }

    private async Task LoadBuiltInContractions()
    {
        try
        {
            var url = Regex.Replace(_appConfigurationProvider.VoiceListUrl, @"voices/?$", "contractions");
            var response = await _downloader.DownloadStringAsync(url);
            var contractions = JsonSerializer.Deserialize(response,
                SourceGenerationContext.NewDefault.DictionaryStringString);
            if (contractions != null)
                BuiltInContractions = new Dictionary<string, string>(contractions, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading built-in contractions");
        }
    }
}
