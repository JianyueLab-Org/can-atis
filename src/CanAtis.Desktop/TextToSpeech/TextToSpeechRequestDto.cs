// <copyright file="TextToSpeechRequestDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace CanAtis.TextToSpeech;

/// <summary>
/// Represents a synthesis request to the CAN TTS service (<c>POST /speech/synthesize</c>).
/// </summary>
public class TextToSpeechRequestDto
{
    /// <summary>
    /// Gets or sets the English (ASCII) text to be converted to speech.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the voice identifier, e.g. <c>huge-harry</c>.
    /// </summary>
    public string? Voice { get; set; }

    /// <summary>
    /// Gets or sets the speech rate in words per minute (120–240).
    /// </summary>
    public int SpeechRate { get; set; } = 180;

    /// <summary>
    /// Gets or sets the CAN ID used to authenticate the request.
    /// </summary>
    public string? Cid { get; set; }

    /// <summary>
    /// Gets or sets the CAN network password used to authenticate the request.
    /// </summary>
    public string? Password { get; set; }
}
