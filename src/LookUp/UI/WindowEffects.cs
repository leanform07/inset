using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LookUp.UI;

/// <summary>Win32 tweaks shared by the floating windows.</summary>
static class WindowEffects
{
    const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_DONOTROUND = 1;
    const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80;

    /// <summary>Square corners (the plate edge) with the system shadow, and no Alt+Tab entry.</summary>
    public static void MakeFloating(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        int corner = DWMWCP_DONOTROUND; // plates have square corners
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, GetWindowLongPtr(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
    }

    const int GWL_STYLE = -16, WS_MAXIMIZEBOX = 0x10000, WS_MINIMIZEBOX = 0x20000;

    /// <summary>
    /// A resizable window gets maximize/minimize styles from WPF; without them, Win+Up and
    /// dragging to the top of the screen cannot maximize a floating plate.
    /// </summary>
    public static void RemoveMaximize(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        SetWindowLongPtr(hwnd, GWL_STYLE, GetWindowLongPtr(hwnd, GWL_STYLE) & ~(nint)(WS_MAXIMIZEBOX | WS_MINIMIZEBOX));
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    static extern nint GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    static extern nint SetWindowLongPtr(IntPtr hwnd, int index, nint value);
}
