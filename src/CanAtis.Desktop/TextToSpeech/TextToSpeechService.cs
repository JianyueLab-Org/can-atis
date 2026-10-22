// <copyright file="TextToSpeechService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CanAtis.Profiles.Models;
using CanAtis.Voice.WavPack;
using Serilog;

namespace CanAtis.TextToSpeech;

/// <summary>
/// Synthesizes speech offline with Windows SAPI, returning 48 kHz mono 16-bit PCM.
/// </summary>
public class TextToSpeechService : ITextToSpeechService
{
    /// <inheritdoc />
    public List<VoiceMetaData> VoiceList { get; private set; } = [];

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> BuiltInContractions { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task Initialize()
    {
        VoiceList = [];
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            VoiceList = await RunSta(() =>
            {
                dynamic voice = CreateComObject("SAPI.SpVoice");
                try
                {
                    dynamic tokens = voice.GetVoices();
                    try
                    {
                        List<VoiceMetaData> voices = [new() { Name = "Default", Id = "default" }];
                        for (var i = 0; i < (int)tokens.Count; i++)
                        {
                            dynamic token = tokens.Item(i);
                            try
                            {
                                voices.Add(new VoiceMetaData { Name = (string)token.GetDescription(), Id = (string)token.Id });
                            }
                            finally
                            {
                                ReleaseComObject(token);
                            }
                        }

                        return voices;
                    }
                    finally
                    {
                        ReleaseComObject(tokens);
                    }
                }
                finally
                {
                    ReleaseComObject(voice);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error enumerating Windows speech voices");
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> RequestAudio(string text, AtisStation station, char atisLetter,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(text))
            return null;

        var voiceName = station.AtisVoice.Voice;
        if (station.AtisVoice.AlternatingVoices is { Length: >= 2 } voices)
        {
            var index = (atisLetter - station.CodeRange.Low) % voices.Length;
            voiceName = voices[index < 0 ? index + voices.Length : index];
        }

        try
        {
            var rate = station.AtisVoice.SpeechRate;
            return await RunSta(() => Synthesize(text, voiceName, rate, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error synthesizing Windows speech with voice {Voice}", voiceName);
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static byte[] Synthesize(string text, string? voiceName, int rate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dynamic voice = CreateComObject("SAPI.SpVoice");
        try
        {
            SelectVoice(voice, voiceName);
            // The existing UI uses 180 as its baseline; SAPI uses -10 through +10.
            voice.Rate = Math.Clamp((rate - 180) / 10, -10, 10);
            var outputPath = Path.Combine(Path.GetTempPath(), $"can-atis-tts-{Guid.NewGuid():N}.wav");
            dynamic stream = CreateComObject("SAPI.SpFileStream");
            try
            {
                stream.Format.Type = 33; // SAFT48kHz16BitMono
                stream.Open(outputPath, 3, false); // SSFMCreateForWrite

                voice.AudioOutputStream = stream;
                voice.Speak(text, 17); // SVSFlagsAsync | SVSFIsNotXML (treat user text as plain text)
                while (!(bool)voice.WaitUntilDone(50))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        voice.Speak(string.Empty, 3); // Async | PurgeBeforeSpeak
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                // Close flushes the samples and finalizes the RIFF data sizes before reading.
                stream.Close();
                var samples = WavDecoder.Decode(File.ReadAllBytes(outputPath));
                var pcm = new byte[samples.Length * sizeof(short)];
                Buffer.BlockCopy(samples, 0, pcm, 0, pcm.Length);
                return pcm;
            }
            finally
            {
                ReleaseComObject(stream);
                try { File.Delete(outputPath); } catch (IOException) { }
            }
        }
        finally
        {
            ReleaseComObject(voice);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SelectVoice(dynamic voice, string? voiceName)
    {
        if (string.IsNullOrWhiteSpace(voiceName) || voiceName == "Default")
            return;

        dynamic tokens = voice.GetVoices();
        try
        {
            for (var i = 0; i < (int)tokens.Count; i++)
            {
                dynamic token = tokens.Item(i);
                try
                {
                    if ((string)token.GetDescription() == voiceName)
                    {
                        voice.Voice = token;
                        return;
                    }
                }
                finally
                {
                    ReleaseComObject(token);
                }
            }

            Log.Warning("Windows voice {Voice} is not installed; using the system default", voiceName);
        }
        finally
        {
            ReleaseComObject(tokens);
        }
    }

    [SupportedOSPlatform("windows")]
    private static Task<T> RunSta<T>(Func<T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(action());
            }
            catch (OperationCanceledException ex)
            {
                completion.SetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }) { IsBackground = true, Name = "Windows TTS" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    [SupportedOSPlatform("windows")]
    private static object CreateComObject(string progId) =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId, true)!)
        ?? throw new InvalidOperationException($"Could not create {progId}.");

    [SupportedOSPlatform("windows")]
    private static void ReleaseComObject(object value)
    {
        if (Marshal.IsComObject(value))
            Marshal.ReleaseComObject(value);
    }
}
