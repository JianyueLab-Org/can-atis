// <copyright file="GetSetAtisLetterReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using CanAtis.Ui.Services.Websocket.Messages;
using WatsonWebsocket;

namespace CanAtis.Events.WebSocket;

/// <summary>
/// Represents an event raised by a websocket client to change the ATIS letter of a station.
/// </summary>
/// <param name="Session">The websocket client making the request.</param>
/// <param name="Payload">The message payload.</param>
public record GetSetAtisLetterReceived(
    ClientMetadata Session,
    SetAtisLetterMessage.SetAtisLetterMessagePayload? Payload) : IEvent;
