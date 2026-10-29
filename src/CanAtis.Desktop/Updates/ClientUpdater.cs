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

namespace CanAtis.Updates;

/// <summary>
/// Provides functionality for checking application updates.
/// </summary>
public class ClientUpdater : IClientUpdater
{
    private readonly HttpClient _httpClient;
    private readonly string _updateCheckUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientUpdater"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">The application configuration provider.</param>
    public ClientUpdater(IAppConfigurationProvider appConfigurationProvider)
    {
        _httpClient = new HttpClient();
        _updateCheckUrl = BuildUpdateCheckUrl(appConfigurationProvider.VersionUrl);
    }

    /// <inheritdoc />
    public async Task<bool> Run()
    {
        EventBus.Instance.Publish(new StartupStatusChanged("Checking for new client version..."));

        if (Debugger.IsAttached)
            return false;

        try
        {
            var response = await _httpClient.GetStringAsync(_updateCheckUrl);
            var updateInfo = JsonSerializer.Deserialize<UpdateResponse>(response);

            if (updateInfo?.Update?.Available == true)
            {
                EventBus.Instance.Publish(new StartupStatusChanged(
                    $"New version available: {updateInfo.Update.Version}"));
                // TODO: Notify user about available update
                // For now, just log and continue - don't block startup
            }
        }
        catch (Exception ex)
        {
            // Silently fail update check - don't block startup
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }

        return false; // Never trigger restart, only notify
    }

    /// <summary>
    /// Builds the update check URL with version and platform parameters.
    /// </summary>
    /// <param name="baseUrl">The base URL from configuration.</param>
    /// <returns>The complete URL with query parameters.</returns>
    internal static string BuildUpdateCheckUrl(string baseUrl)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString(3) ?? "0.0.0";

        var uriBuilder = new UriBuilder(baseUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        query["version"] = version;

        if (OperatingSystem.IsWindows())
            query["platform"] = "windows";
        else if (OperatingSystem.IsMacOS())
            query["platform"] = "macos";
        else if (OperatingSystem.IsLinux())
            query["platform"] = "linux";

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }

    private sealed class UpdateResponse
    {
        public UpdateInfo? Update { get; set; }
    }

    private sealed class UpdateInfo
    {
        public bool Available { get; set; }

        public string? Version { get; set; }
    }
}
