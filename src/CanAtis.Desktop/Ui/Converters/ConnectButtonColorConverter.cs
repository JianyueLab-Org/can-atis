// <copyright file="ConnectButtonColorConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CanAtis.Networking;

namespace CanAtis.Ui.Converters;

/// <summary>
/// Converts a <see cref="NetworkConnectionStatus" /> to a corresponding color string
/// representing different network statuses for styling purposes in the user interface.
/// </summary>
public class ConnectButtonColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkConnectionStatus flag)
        {
            return flag == NetworkConnectionStatus.Connected ? "#30333A" : "#4C8BF5";
        }

        return "#4C8BF5";
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
