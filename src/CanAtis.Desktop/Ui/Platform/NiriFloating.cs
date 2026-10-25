// Adapted from can-client-ui AtisClient, revision 7039d3f.
using System;
using Avalonia.Controls;
using Avalonia.Threading;

namespace CanAtis.Ui.Platform;

/// <summary>Opens resizable main windows as floating windows under niri/XWayland.</summary>
internal static class NiriFloating
{
    public static void Apply(Window window)
    {
        if (!OperatingSystem.IsLinux()
            || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NIRI_SOCKET"))
            || !window.CanResize)
            return;

        window.CanResize = false;
        var restored = false;
        void Restore()
        {
            if (restored)
                return;

            restored = true;
            window.Activated -= OnActivated;
            window.Opened -= OnOpened;
            window.Closed -= OnClosed;
            window.CanResize = true;
        }

        void OnActivated(object? sender, EventArgs e) => Restore();
        void OnOpened(object? sender, EventArgs e) => DispatcherTimer.RunOnce(Restore, TimeSpan.FromSeconds(3));
        void OnClosed(object? sender, EventArgs e) => Restore();
        window.Activated += OnActivated;
        window.Opened += OnOpened;
        window.Closed += OnClosed;
    }
}
