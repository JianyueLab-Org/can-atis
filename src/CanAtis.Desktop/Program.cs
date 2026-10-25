// <copyright file="Program.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Media;
using Serilog;
using Velopack;

namespace CanAtis;

/// <summary>
/// The main entry point for the application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Gets a value indicating whether the application has been updated, and the release notes should be displayed.
    /// </summary>
    public static bool IsUpdated { get; private set; }

    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            if (Array.IndexOf(args, "--self-test") >= 0)
            {
                CanAtis.Io.PathProvider.SetAppDataPath(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ceruleanavi.atis-for-can"));
                using var airports = typeof(Program).Assembly.GetManifestResourceStream("CanAtis.Airports.json")
                    ?? throw new InvalidOperationException("Bundled airports missing");
                CanAtis.Voice.Audio.NativeAudio.Initialize();
                if (!CanAtis.Voice.Audio.NativeAudio.IsAvailable)
                    throw new InvalidOperationException("Native audio unavailable");
                var worker = CanAtis.Io.BundledVoiceWorker.GetPath()
                    ?? throw new InvalidOperationException("Bundled voice worker missing");
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(worker, "--self-test")
                    { UseShellExecute = false, CreateNoWindow = true });
                if (process == null || !process.WaitForExit(30000) || process.ExitCode != 0)
                    throw new InvalidOperationException("Voice worker self-test failed");
                return;
            }
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            VelopackApp.Build().OnFirstRun(OnFirstRun).OnRestarted(OnRestarted).Run();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled Exception");
            Environment.ExitCode = 1;
        }

        Log.CloseAndFlush();
    }

    private static void OnRestarted(SemanticVersion version)
    {
        IsUpdated = true;
    }

    private static void OnFirstRun(SemanticVersion version)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var upgradeAssistantPath = Path.Combine(localAppData, "vatis-upgrade-assistant");
            if (Directory.Exists(upgradeAssistantPath))
            {
                Directory.Delete(upgradeAssistantPath, recursive: true);
                Log.Information("Deleted upgrade assistant directory: {UpgradeAssistantPath}", upgradeAssistantPath);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete upgrade assistant directory");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions { WmClass = "can-atis-client", EnableSessionManagement = false })
            .With(new FontManagerOptions
            {
                FontFallbacks =
                [
                    new() { FontFamily = new FontFamily("Microsoft YaHei UI") },
                    new() { FontFamily = new FontFamily("PingFang SC") },
                    new() { FontFamily = new FontFamily("Hiragino Sans GB") },
                    new() { FontFamily = new FontFamily("Noto Sans CJK SC") },
                    new() { FontFamily = new FontFamily("Noto Sans SC") },
                    new() { FontFamily = new FontFamily("Source Han Sans SC") },
                    new() { FontFamily = new FontFamily("WenQuanYi Micro Hei") },
                    new() { FontFamily = new FontFamily("Segoe UI Symbol") },
                    new() { FontFamily = new FontFamily("Apple Symbols") },
                    new() { FontFamily = new FontFamily("DejaVu Sans") },
                    new() { FontFamily = new FontFamily("Noto Sans Symbols 2") },
                    new() { FontFamily = new FontFamily("Noto Sans Symbols") },
                ]
            })
            .UseReactiveUI()
            .LogToTrace();
}
