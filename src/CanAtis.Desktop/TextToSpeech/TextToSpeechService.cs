// <copyright file="TextToSpeechService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CanAtis.Atis;
using CanAtis.Config;
using CanAtis.Profiles.Models;
using Serilog;

namespace CanAtis.TextToSpeech;

/// <summary>
/// Synthesizes speech with the CAN TTS service (can-tts, DECtalk "Huge Harry"), which returns raw
/// 48 kHz mono signed 16-bit little-endian PCM. Each request is authenticated with the user's CAN ID and
/// network password; the service verifies them against the CAN auth API.
/// </summary>
public class TextToSpeechService : ITextToSpeechService
{
    /// <summary>The default synthesis endpoint.</summary>
    public const string DefaultSynthesisUrl = "https://tts.ceruleanavi.net/speech/synthesize";

    /// <summary>The default voice catalogue endpoint.</summary>
    public const string DefaultVoicesUrl = "https://tts.ceruleanavi.net/speech/voices";

    private const string DefaultVoiceId = "huge-harry";
    private const int MinSpeechRate = 120;
    private const int MaxSpeechRate = 240;
    private const int MaxTextLength = 10_000;
    private const int MaxPcmBytes = 32 * 1024 * 1024;
    private const int MaxBusyRetries = 2;
    private static readonly TimeSpan s_defaultRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(10);

    private static readonly VoiceMetaData s_fallbackVoice = new() { Name = "Huge Harry", Id = DefaultVoiceId };

    private readonly IAppConfig _appConfig;
    private readonly HttpClient _httpClient;
    private readonly string _synthesisUrl;
    private readonly string _voicesUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextToSpeechService"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration holding the CAN ID and network password.</param>
    public TextToSpeechService(IAppConfig appConfig)
    {
        _appConfig = appConfig;
        _synthesisUrl = ReadUrl("CAN_TTS_URL", DefaultSynthesisUrl);
        _voicesUrl = ReadUrl("CAN_TTS_VOICES_URL", DefaultVoicesUrl);

        // Synthesis can take up to 30 seconds server-side; allow a margin beyond that.
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        var version = typeof(TextToSpeechService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0";
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CanAtis/" + version);
    }

    /// <inheritdoc />
    public List<VoiceMetaData> VoiceList { get; private set; } = [s_fallbackVoice];

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> BuiltInContractions { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task Initialize()
    {
        try
        {
            using var response = await _httpClient.GetAsync(_voicesUrl);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var voices = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.ListVoiceMetaData);
            var valid = voices?.Where(v => !string.IsNullOrWhiteSpace(v.Name) && !string.IsNullOrWhiteSpace(v.Id))
                .ToList();
            VoiceList = valid is { Count: > 0 } ? valid : [s_fallbackVoice];
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load the CAN TTS voice list from {Url}; using Huge Harry", _voicesUrl);
            VoiceList = [s_fallbackVoice];
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> RequestAudio(string text, AtisStation station, char atisLetter,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var spoken = PrepareText(text);

        var cid = _appConfig.UserId?.Trim();
        var password = _appConfig.PasswordDecrypted;
        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(password))
            throw new AtisBuilderException("Text to speech requires your CAN ID and network password in user settings.");

        var voiceName = station.AtisVoice.Voice;
        if (station.AtisVoice.AlternatingVoices is { Length: >= 2 } voices)
        {
            var index = (atisLetter - station.CodeRange.Low) % voices.Length;
            voiceName = voices[index < 0 ? index + voices.Length : index];
        }

        var request = new TextToSpeechRequestDto
        {
            Text = spoken,
            Voice = ResolveVoiceId(voiceName),
            SpeechRate = Math.Clamp(station.AtisVoice.SpeechRate, MinSpeechRate, MaxSpeechRate),
            Cid = cid,
            Password = password,
        };
        var json = JsonSerializer.Serialize(request, SourceGenerationContext.NewDefault.TextToSpeechRequestDto);

        for (var attempt = 0; ; attempt++)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, _synthesisUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(message, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new AtisBuilderException("The CAN TTS service timed out.");
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "Could not reach the CAN TTS service at {Url}", _synthesisUrl);
                throw new AtisBuilderException($"Could not reach the CAN TTS service: {ex.Message}");
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                    return await ReadPcm(response, cancellationToken);

                var error = await ReadError(response, cancellationToken);
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < MaxBusyRetries)
                {
                    // Never trust the server's Retry-After blindly; keep the wait short and bounded.
                    var delay = response.Headers.RetryAfter?.Delta ?? s_defaultRetryDelay;
                    if (delay < TimeSpan.Zero || delay > s_maxRetryDelay)
                        delay = delay < TimeSpan.Zero ? s_defaultRetryDelay : s_maxRetryDelay;

                    await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)), cancellationToken);
                    continue;
                }

                Log.Error("CAN TTS request failed: HTTP {Status} {Error}", (int)response.StatusCode, error);
                throw new AtisBuilderException(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized =>
                        "The CAN TTS service rejected your CAN ID or network password. Check your user settings.",
                    HttpStatusCode.BadRequest => $"The CAN TTS service rejected the request: {error}",
                    HttpStatusCode.ServiceUnavailable => "The CAN TTS service is busy or not ready. Try again shortly.",
                    HttpStatusCode.GatewayTimeout => "The CAN TTS service timed out. Try a shorter ATIS.",
                    _ => $"The CAN TTS service failed (HTTP {(int)response.StatusCode}): {error}",
                });
            }
        }
    }

    /// <summary>
    /// Makes ATIS text acceptable to the service: ASCII only, no square brackets (which DECtalk would
    /// interpret as inline commands), at most 10,000 characters.
    /// </summary>
    /// <param name="text">The text to prepare.</param>
    /// <returns>The prepared text.</returns>
    public static string PrepareText(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormKD))
        {
            if (c is '[' or ']')
            {
                builder.Append(' ');
            }
            else if (c is '\r' or '\n' or '\t' || (c >= ' ' && c <= '~'))
            {
                builder.Append(c);
            }
            else if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                // Drop combining accents left over from normalization (e.g. "é" -> "e").
            }
            else if (char.IsWhiteSpace(c))
            {
                builder.Append(' ');
            }
            else
            {
                throw new AtisBuilderException(
                    "The CAN TTS service only supports English text. Remove non-English characters " +
                    $"(such as '{c}') from the voice ATIS, or use a WAV voice pack.");
            }
        }

        var result = builder.ToString().Trim();
        if (result.Length == 0)
            throw new AtisBuilderException("The voice ATIS has no speakable English text.");

        if (result.Length > MaxTextLength)
            throw new AtisBuilderException($"The voice ATIS is longer than {MaxTextLength} characters.");

        return result;
    }

    private string ResolveVoiceId(string? voiceName)
    {
        if (!string.IsNullOrWhiteSpace(voiceName))
        {
            var match = VoiceList.FirstOrDefault(v =>
                string.Equals(v.Name, voiceName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.Id, voiceName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match.Id;

            if (!string.Equals(voiceName, "Default", StringComparison.OrdinalIgnoreCase))
                Log.Warning("Voice {Voice} is not provided by the CAN TTS service; using {Fallback}", voiceName,
                    VoiceList[0].Name);
        }

        return VoiceList.Count > 0 ? VoiceList[0].Id : DefaultVoiceId;
    }

    private static async Task<byte[]> ReadPcm(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
            throw new AtisBuilderException($"The CAN TTS service returned an unexpected response type ({mediaType}).");

        var pcm = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (pcm.Length == 0 || pcm.Length % 2 != 0 || pcm.Length > MaxPcmBytes)
            throw new AtisBuilderException("The CAN TTS service returned invalid audio.");

        return pcm; // 48 kHz, mono, signed 16-bit little-endian.
    }

    private static async Task<string> ReadError(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String)
            {
                return error.GetString() ?? response.ReasonPhrase ?? "";
            }
        }
        catch (Exception ex) when (ex is JsonException or HttpRequestException or InvalidOperationException)
        {
            // Not a JSON error body.
        }

        return response.ReasonPhrase ?? "";
    }

    private static string ReadUrl(string variable, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(variable)?.Trim();
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.ToString()
            : fallback;
    }
}
