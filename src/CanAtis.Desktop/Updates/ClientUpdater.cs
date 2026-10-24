// <copyright file="ClientUpdater.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using CanAtis.Config;
using CanAtis.Events;
using CanAtis.Events.EventBus;
using Serilog;

namespace CanAtis.Updates;

/// <summary>
/// Provides functionality for checking client application updates.
/// </summary>
public class ClientUpdater : IClientUpdater
{
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private readonly HttpClient _httpClient;
    private UpdateCheckResponse? _updateInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientUpdater"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">The application configuration provider.</param>
    public ClientUpdater(IAppConfigurationProvider appConfigurationProvider)
    {
        _appConfigurationProvider = appConfigurationProvider;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <inheritdoc />
    public async Task<bool> Run()
    {
        EventBus.Instance.Publish(new StartupStatusChanged("检查客户端更新..."));

        if (Debugger.IsAttached)
        {
            Log.Debug("Debugger attached, skipping update check");
            return false;
        }

        try
        {
            var currentVersion = GetCurrentVersion();
            var updateUrl = $"{_appConfigurationProvider.VersionUrl}?client=atis-for-can&version={currentVersion}";

            Log.Information("Checking for updates from {UpdateUrl}", updateUrl);

            var response = await _httpClient.GetStringAsync(updateUrl);
            _updateInfo = JsonSerializer.Deserialize<UpdateCheckResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (_updateInfo?.Available == true)
            {
                Log.Information("Update available: {LatestVersion}", _updateInfo.LatestVersion);
                EventBus.Instance.Publish(new StartupStatusChanged($"有新版本可用: {_updateInfo.LatestVersion}"));

                // Notify user about the update availability
                // The UI should handle displaying this information and providing download link
                return true;
            }

            Log.Information("No updates available, current version {CurrentVersion} is up to date", currentVersion);
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for updates");
            return false;
        }
    }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    /// <returns>The current version string.</returns>
    private static string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";

        // Remove git commit hash if present
        var plusIndex = version.IndexOf('+');
        if (plusIndex > 0)
        {
            version = version.Substring(0, plusIndex);
        }

        return version;
    }

    /// <summary>
    /// Response from the update check endpoint.
    /// </summary>
    private class UpdateCheckResponse
    {
        public bool Available { get; set; }
        public string? LatestVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ReleaseNotes { get; set; }
    }
}
