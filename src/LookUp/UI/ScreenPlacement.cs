using System.Windows;
using System.Windows.Interop;

namespace LookUp.UI;

/// <summary>
/// Where a floating window goes. Everything here is in physical screen pixels, because WPF's
/// Left/Top are relative to the DPI of the monitor the window is on now, not the one it is going to.
/// </summary>
static class ScreenPlacement
{
    // Offsets from the cursor, in DIPs: just under the selected word, a little to its left.
    const double CursorDx = -24, CursorDy = 22;

    public readonly record struct Monitor(Rect WorkArea, double Scale);

    /// <summary>Where a window should open: next to the cursor, or with its top-left corner at a point.</summary>
    public readonly record struct Anchor(Point Point, bool AtCursor)
    {
        public static Anchor Cursor(Point physical) => new(physical, true);
        public static Anchor TopLeft(Point physical) => new(physical, false);
    }

    /// <summary>Moves the window for the anchor, using the size it will have on the anchor's monitor.</summary>
    public static void Place(Window window, Anchor anchor)
    {
        var monitor = MonitorAt(anchor.Point);
        var size = SizeOn(window, monitor);
        var rect = anchor.AtCursor ? NearCursor(anchor.Point, size, monitor) : AtTopLeft(anchor.Point, size, monitor);
        MoveTo(window, rect.TopLeft);
    }

    /// <summary>
    /// Below-right of the cursor; flips above when there is no room below and left when there is
    /// no room to the right; always kept inside the work area.
    /// </summary>
    public static Rect NearCursor(Point cursor, Size size, Monitor monitor)
    {
        var area = monitor.WorkArea;
        var dx = CursorDx * monitor.Scale;
        var dy = CursorDy * monitor.Scale;

        var x = cursor.X + dx;
        var y = cursor.Y + dy;
        if (x + size.Width > area.Right) x = cursor.X - size.Width - dx;
        if (y + size.Height > area.Bottom) y = cursor.Y - size.Height - dy;
        return Clamp(new Rect(x, y, size.Width, size.Height), area);
    }

    /// <summary>At the given top-left corner, moved back inside the work area if needed.</summary>
    public static Rect AtTopLeft(Point topLeft, Size size, Monitor monitor) =>
        Clamp(new Rect(topLeft, size), monitor.WorkArea);

    static Rect Clamp(Rect rect, Rect area)
    {
        var x = Math.Max(area.Left, Math.Min(rect.X, area.Right - rect.Width));
        var y = Math.Max(area.Top, Math.Min(rect.Y, area.Bottom - rect.Height));
        return new Rect(x, y, rect.Width, rect.Height);
    }

    // ── Screen queries ────────────────────────────────────────

    public static Point Cursor()
    {
        Native.GetCursorPos(out var p);
        return new Point(p.X, p.Y);
    }

    public static Monitor MonitorAt(Point physical)
    {
        var handle = Native.MonitorFromPoint(new Native.POINT { X = (int)physical.X, Y = (int)physical.Y }, Native.MONITOR_DEFAULTTONEAREST);
        var info = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Native.MONITORINFO>() };
        Native.GetMonitorInfo(handle, ref info);
        var scale = Native.GetDpiForMonitor(handle, 0 /* effective */, out var dpi, out _) == 0 ? dpi / 96.0 : 1.0;
        var w = info.rcWork;
        return new Monitor(new Rect(w.Left, w.Top, w.Right - w.Left, w.Bottom - w.Top), scale);
    }

    public static Rect WindowRect(Window window)
    {
        Native.GetWindowRect(new WindowInteropHelper(window).EnsureHandle(), out var r);
        return new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
    }

    /// <summary>Physical size the window will have on that monitor.</summary>
    public static Size SizeOn(Window window, Monitor monitor)
    {
        var width = double.IsNaN(window.Width) ? window.ActualWidth : window.Width;
        var height = double.IsNaN(window.Height) || window.SizeToContent != SizeToContent.Manual ? window.ActualHeight : window.Height;
        return new Size(width * monitor.Scale, height * monitor.Scale);
    }

    public static void MoveTo(Window window, Point physicalTopLeft) =>
        Native.SetWindowPos(new WindowInteropHelper(window).EnsureHandle(), IntPtr.Zero,
            (int)Math.Round(physicalTopLeft.X), (int)Math.Round(physicalTopLeft.Y), 0, 0,
            Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
}
