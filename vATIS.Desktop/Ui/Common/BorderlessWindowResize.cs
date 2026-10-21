// <copyright file="BorderlessWindowResize.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// Edge resizing for windows drawn with <c>SystemDecorations="None"</c>, which have no native resize border.
/// </summary>
internal static class BorderlessWindowResize
{
    private const double EdgeSize = 8;

    // Cursors wrap a platform cursor; build each one once rather than on every pointer move.
    private static readonly Cursor s_northSouth = new(StandardCursorType.SizeNorthSouth);
    private static readonly Cursor s_westEast = new(StandardCursorType.SizeWestEast);
    private static readonly Cursor s_topLeft = new(StandardCursorType.TopLeftCorner);
    private static readonly Cursor s_topRight = new(StandardCursorType.TopRightCorner);
    private static readonly Cursor s_bottomLeft = new(StandardCursorType.BottomLeftCorner);
    private static readonly Cursor s_bottomRight = new(StandardCursorType.BottomRightCorner);

    /// <summary>
    /// Starts a resize drag if the left button was pressed within <see cref="EdgeSize"/> of the window's edge.
    /// </summary>
    /// <param name="window">The window to resize.</param>
    /// <param name="e">The pointer event.</param>
    /// <returns><c>true</c> if a resize drag was started and the event handled; otherwise, <c>false</c>.</returns>
    public static bool TryBeginResizeDrag(Window window, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
            return false;

        var edge = GetEdge(window, e.GetPosition(window));
        if (edge is null)
            return false;

        window.BeginResizeDrag(edge.Value, e);
        e.Handled = true;
        return true;
    }

    /// <summary>
    /// Shows the matching resize cursor while the pointer is over an edge, and the default cursor elsewhere.
    /// </summary>
    /// <param name="window">The window under the pointer.</param>
    /// <param name="e">The pointer event.</param>
    public static void UpdateCursor(Window window, PointerEventArgs e)
    {
        var cursor = GetCursor(GetEdge(window, e.GetPosition(window)));
        if (!ReferenceEquals(window.Cursor, cursor))
            window.Cursor = cursor;
    }

    /// <summary>
    /// Restores the default cursor, for when the pointer leaves the window.
    /// </summary>
    /// <param name="window">The window.</param>
    public static void ResetCursor(Window window) => window.Cursor = null;

    private static WindowEdge? GetEdge(Window window, Point position)
    {
        if (!window.CanResize || window.WindowState != WindowState.Normal)
            return null;

        var left = position.X <= EdgeSize;
        var right = position.X >= window.Bounds.Width - EdgeSize;
        var top = position.Y <= EdgeSize;
        var bottom = position.Y >= window.Bounds.Height - EdgeSize;

        return (left, top, right, bottom) switch
        {
            (true, true, false, false) => WindowEdge.NorthWest,
            (false, true, true, false) => WindowEdge.NorthEast,
            (true, false, false, true) => WindowEdge.SouthWest,
            (false, false, true, true) => WindowEdge.SouthEast,
            (_, true, _, false) => WindowEdge.North,
            (_, false, _, true) => WindowEdge.South,
            (true, false, false, false) => WindowEdge.West,
            (false, false, true, false) => WindowEdge.East,
            _ => null,
        };
    }

    private static Cursor? GetCursor(WindowEdge? edge) => edge switch
    {
        WindowEdge.North or WindowEdge.South => s_northSouth,
        WindowEdge.West or WindowEdge.East => s_westEast,
        WindowEdge.NorthWest => s_topLeft,
        WindowEdge.NorthEast => s_topRight,
        WindowEdge.SouthWest => s_bottomLeft,
        WindowEdge.SouthEast => s_bottomRight,
        _ => null,
    };
}
