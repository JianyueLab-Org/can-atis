// <copyright file="NetworkStatus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CanAtis.Config;

/// <summary>
/// Represents the network status configuration, including properties for retrieving METAR URLs.
/// </summary>
public class NetworkStatus
{
    /// <summary>
    /// Gets or sets the list of METAR URLs retrieved from the network status configuration.
    /// </summary>
    [JsonPropertyName("metar")]
    public List<string> MetarUrls { get; set; } = [];
}
