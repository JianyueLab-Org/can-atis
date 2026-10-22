// <copyright file="NavDataUnavailableException.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace CanAtis.NavData;

/// <summary>
/// Thrown when required navigation data is missing and could not be downloaded.
/// </summary>
public class NavDataUnavailableException() : Exception(
    "CAN ATIS could not load the required airport data. " +
    "Please check the bundled data files and restart CAN ATIS, " +
    "then restart the application.");
