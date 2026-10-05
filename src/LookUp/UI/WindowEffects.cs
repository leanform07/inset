using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LookUp.UI;

/// <summary>Win32 tweaks shared by the floating windows.</summary>
static class WindowEffects
{
    const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2;
    const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80;

    /// <summary>Rounded Windows 11 corners, and no Alt+Tab entry.</summary>
    public static void MakeFloating(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        int corner = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, GetWindowLongPtr(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    static extern nint GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    static extern nint SetWindowLongPtr(IntPtr hwnd, int index, nint value);
}
