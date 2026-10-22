// <copyright file="AtisConfigurationWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Serilog;
using CanAtis.Profiles.Models;
using CanAtis.Ui.Dialogs.MessageBox;
using CanAtis.Ui.ViewModels;

namespace CanAtis.Ui.Windows;

/// <summary>
/// Represents the ATIS configuration window.
/// </summary>
public partial class AtisConfigurationWindow : ReactiveWindow<AtisConfigurationWindowViewModel>, ICloseable,
    IDialogOwner
{
    private int _currentStationIndex;
    private bool _suppressSelectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model to be used as the data context for this window.</param>
    public AtisConfigurationWindow(AtisConfigurationWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel?.Initialize(this);
        Closed += OnClosed;

        this.WhenAnyValue(x => x.ViewModel!.SelectedAtisStation).Subscribe(station =>
        {
            var index = Stations.Items.IndexOf(station);
            Stations.SelectedIndex = index;
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    public AtisConfigurationWindow()
    {
        InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private async void Stations_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (Stations.SelectedIndex < 0)
                return;

            if (_suppressSelectionChanged)
                return;

            if (ViewModel is not { } model)
                return;

            if (model.HasUnsavedChanges && Stations.SelectedIndex != _currentStationIndex)
            {
                var result = await MessageBox.ShowDialog(this,
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information);

                if (result == MessageBoxResult.No)
                {
                    _suppressSelectionChanged = true;
                    Stations.SelectedIndex = _currentStationIndex;
                    _suppressSelectionChanged = false;

                    e.Handled = true;
                    return;
                }

                model.HasUnsavedChanges = false;
            }

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisStation station)
            {
                await model.SelectedAtisStationChanged.Execute(station);
                _currentStationIndex = Stations.SelectedIndex;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while loading composites");
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TryBeginResizeDrag(e))
        {
            return;
        }

        if (e.Source is Border { Name: "TitleBar" } && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
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

    private bool TryBeginResizeDrag(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || WindowState == WindowState.Maximized)
        {
            return false;
        }

        var edge = GetResizeEdge(e.GetPosition(this));
        if (edge is null)
        {
            return false;
        }

        BeginResizeDrag(edge.Value, e);
        e.Handled = true;
        return true;
    }

    private WindowEdge? GetResizeEdge(Point position)
    {
        const double edgeSize = 8;
        var nearLeft = position.X <= edgeSize;
        var nearRight = position.X >= Bounds.Width - edgeSize;
        var nearTop = position.Y <= edgeSize;
        var nearBottom = position.Y >= Bounds.Height - edgeSize;

        return (nearLeft, nearTop, nearRight, nearBottom) switch
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
    }

    private Cursor GetResizeCursor(Point position)
    {
        if (WindowState == WindowState.Maximized)
        {
            return new Cursor(StandardCursorType.Arrow);
        }

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
}
