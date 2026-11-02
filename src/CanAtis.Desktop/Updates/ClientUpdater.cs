// <copyright file="ClientUpdater.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using CanAtis.Config;
using CanAtis.Events;
using CanAtis.Events.EventBus;
using CanAtis.Io;
using Serilog;

namespace CanAtis.Updates;

/// <summary>
/// Checks CAN release metadata without downloading or replacing application files.
/// </summary>
public class ClientUpdater : IClientUpdater
{
    private readonly IAppConfigurationProvider _configuration;
    private readonly IDownloader _downloader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientUpdater"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">The application configuration provider.</param>
    /// <param name="downloader">The HTTP downloader.</param>
    public ClientUpdater(IAppConfigurationProvider appConfigurationProvider, IDownloader downloader)
    {
        _configuration = appConfigurationProvider;
        _downloader = downloader;
    }

    /// <inheritdoc />
    public async Task<ClientUpdate?> Run()
    {
        EventBus.Instance.Publish(new StartupStatusChanged("Checking for new client version..."));
        try
        {
            var current = typeof(ClientUpdater).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? throw new InvalidOperationException("Application version not found");
            var url = _configuration.VersionUrl;
            var joiner = url.Contains('?') ? "&" : "?";
            using var response = await _downloader.GetAsync($"{url}{joiner}version={Uri.EscapeDataString(current)}");
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("update", out var update) || update.ValueKind != JsonValueKind.Object
                || !update.TryGetProperty("available", out var available) || available.ValueKind != JsonValueKind.True
                || !update.TryGetProperty("latest", out var latest) || latest.ValueKind != JsonValueKind.String)
                return null;

            var offered = latest.GetString()?.Trim();
            // The API verdict is required; also reject stale metadata that would offer a downgrade.
            if (!Version.TryParse(offered?.TrimStart('v'), out var version)
                || !Version.TryParse(current.Split('+')[0].Split('-')[0], out var installed)
                || version <= installed)
                return null;

            return new ClientUpdate(offered!,
                $"https://api.ceruleanavi.net/api/v1/clients/download/atis-for-can?v={Uri.EscapeDataString(offered!)}");
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Client update check unavailable");
            return null;
        }
    }
}

/// <summary>
/// A newer CAN release and its download relay link.
/// </summary>
/// <param name="Version">The offered version.</param>
/// <param name="DownloadUrl">The CAN relay download URL.</param>
public sealed record ClientUpdate(string Version, string DownloadUrl);
