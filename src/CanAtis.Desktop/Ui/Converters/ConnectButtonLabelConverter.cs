// <copyright file="ConnectButtonLabelConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CanAtis.Networking;

namespace CanAtis.Ui.Converters;

/// <summary>
/// Converts the <see cref="NetworkConnectionStatus"/> value to an appropriate label string
/// for the connect button in the user interface.
/// </summary>
public class ConnectButtonLabelConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkConnectionStatus flag)
        {
            return flag switch
            {
                NetworkConnectionStatus.Connected => "断开连接",
                NetworkConnectionStatus.Connecting => "连接中",
                NetworkConnectionStatus.Disconnected => "连接",
                _ => "连接",
            };
        }

        return "连接";
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
