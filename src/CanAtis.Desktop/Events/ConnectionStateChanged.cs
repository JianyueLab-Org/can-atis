// <copyright file="ConnectionStateChanged.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using CanAtis.Networking.AtisHub;

namespace CanAtis.Events;

/// <summary>
/// Represents an event that is raised when the connection state changes.
/// </summary>
/// <param name="ConnectionState">The new connection state.</param>
public record ConnectionStateChanged(ConnectionState ConnectionState) : IEvent;
