// <copyright file="MainWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Serilog;
using CanAtis.Ui.Controls.Notification;
using CanAtis.Ui.Extensions;
using CanAtis.Ui.ViewModels;

namespace CanAtis.Ui.Windows;

/// <summary>
/// Represents the main window of the application, providing the primary UI and functionality for user interaction.
/// </summary>
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with the main window.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        SetWindowsIcon();

        NotificationManager = new WindowNotificationManager(this) { MaxItems = 4 };

        ViewModel = viewModel;
        ViewModel.Owner = this;
        RestorePositionBeforeShow();

        Opened += OnOpened;
        Closed += OnClosed;
        Closing += OnClosing;
        KeyDown += OnWindowKeyDown;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        SetWindowsIcon();
    }

    private void SetWindowsIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "MainIcon.ico");
            if (File.Exists(path))
                Icon = new WindowIcon(path);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "could not set the CAN ATIS window icon");
        }
    }

    /// <summary>
    /// Gets the notification manager.
    /// </summary>
    public WindowNotificationManager? NotificationManager { get; }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;

        if (WindowStartupLocation != WindowStartupLocation.Manual)
        {
            ViewModel?.RestorePosition(this);
        }
    }

    private void RestorePositionBeforeShow()
    {
        // Restore the saved position before the window is shown so it doesn't appear centered and then jump.
        var defaultPosition = Position;
        ViewModel?.RestorePosition(this);
        if (Position != defaultPosition)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            // Check if the window close request was triggered by the user (e.g., ALT+F4 or similar)
            if (ViewModel is null || e.IsProgrammatic)
                return;

            e.Cancel = true;

            var shouldClose = await ViewModel.EndClientSessionCommand.Execute().FirstAsync();
            if (shouldClose)
            {
                e.Cancel = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnClosing Exception");
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_initialized)
            return;

        WindowNotificationManager.TryGetNotificationManager(this, out var manager);
        if (manager != null)
        {
            if (ViewModel != null)
            {
                manager.Position = Avalonia.Controls.Notifications.NotificationPosition.TopLeft;
                ViewModel.NotificationManager = manager;
            }
        }

        ViewModel?.PopulateAtisStations();
        ViewModel?.ConnectToHub();

        _initialized = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        Closed -= OnClosed;
        Closing -= OnClosing;
        KeyDown -= OnWindowKeyDown;
        PositionChanged -= OnPositionChanged;

        ViewModel?.DisconnectFromHub();
        ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (TryBeginResizeDrag(e))
            return;

        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TryBeginResizeDrag(e);
    }

    private void OnWindowPointerMoved(object? sender, PointerEventArgs e)
    {
        Cursor = GetResizeCursor(e.GetPosition(this));
    }

    private void OnWindowPointerExited(object? sender, PointerEventArgs e)
    {
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private Cursor GetResizeCursor(Point position)
    {
        if (WindowState == WindowState.Maximized)
            return new Cursor(StandardCursorType.Arrow);

        const double edgeSize = 8;
        var nearLeft = position.X <= edgeSize;
        var nearRight = position.X >= Bounds.Width - edgeSize;
        var nearTop = position.Y <= edgeSize;
        var nearBottom = position.Y >= Bounds.Height - edgeSize;

        var cursorType = (nearLeft, nearTop, nearRight, nearBottom) switch
        {
            (true, true, false, false) or (false, false, true, true) => StandardCursorType.TopLeftCorner,
            (false, true, true, false) or (true, false, false, true) => StandardCursorType.TopRightCorner,
            (true, false, false, false) or (false, false, true, false) => StandardCursorType.SizeWestEast,
            (_, true, _, false) or (_, false, _, true) => StandardCursorType.SizeNorthSouth,
            _ => StandardCursorType.Arrow
        };

        return new Cursor(cursorType);
    }

    private bool TryBeginResizeDrag(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || WindowState == WindowState.Maximized)
            return false;

        var position = e.GetPosition(this);
        const double edgeSize = 8;
        var nearLeft = position.X <= edgeSize;
        var nearRight = position.X >= Bounds.Width - edgeSize;
        var nearTop = position.Y <= edgeSize;
        var nearBottom = position.Y >= Bounds.Height - edgeSize;

        WindowEdge? edge = (nearLeft, nearTop, nearRight, nearBottom) switch
        {
            (true, true, false, false) => WindowEdge.NorthWest,
            (false, true, true, false) => WindowEdge.NorthEast,
            (true, false, false, true) => WindowEdge.SouthWest,
            (false, false, true, true) => WindowEdge.SouthEast,
            (_, true, _, false) => WindowEdge.North,
            (true, false, false, false) => WindowEdge.West,
            (false, false, true, false) => WindowEdge.East,
            (_, false, _, true) => WindowEdge.South,
            _ => null
        };

        if (edge is null)
            return false;

        BeginResizeDrag(edge.Value, e);
        e.Handled = true;
        return true;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        AppDataShortcut.HandleKeyDown(this, e);
    }

    private void OnMinimizeWindow(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        ViewModel?.UpdatePosition(this);
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.InvokeMiniWindowCommand.Execute().Subscribe();
    }
}
