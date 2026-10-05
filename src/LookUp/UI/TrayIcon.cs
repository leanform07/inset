using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace LookUp.UI;

/// <summary>
/// The notification-area icon. Left click opens search; right click shows a WPF menu,
/// so it follows the app theme instead of the old Windows Forms look.
/// </summary>
sealed class TrayIcon : IDisposable
{
    readonly Forms.NotifyIcon _icon;
    readonly ContextMenu _menu = new();
    readonly MenuItem _searchItem, _notebookItem;
    readonly HwndSource _focusSink; // the menu needs a foreground window of ours to close on outside clicks

    public TrayIcon(Action search, Action notebook, Action settings, Action quit)
    {
        _searchItem = Item("Look up a word…", search);
        _notebookItem = Item("Notebook", notebook);
        _menu.Items.Add(_searchItem);
        _menu.Items.Add(_notebookItem);
        _menu.Items.Add(new Separator());
        _menu.Items.Add(Item("How to use", UserGuide.Open));
        _menu.Items.Add(Item("Settings…", settings));
        _menu.Items.Add(Item("Quit Inset", quit));

        _focusSink = new HwndSource(new HwndSourceParameters("InsetTray") { Width = 0, Height = 0, WindowStyle = unchecked((int)0x80000000) }); // WS_POPUP, never shown

        using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Inset;component/Assets/AppIcon.ico")).Stream;
        _icon = new Forms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize),
            Text = "Inset",
            Visible = true,
        };
        _icon.MouseUp += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) search();
            else if (e.Button == Forms.MouseButtons.Right) ShowMenu();
        };
    }

    /// <summary>Shows the current shortcuts next to the menu items.</summary>
    public void SetShortcuts(string search, string notebook)
    {
        _searchItem.InputGestureText = search;
        _notebookItem.InputGestureText = notebook;
    }

    public void ShowMessage(string title, string text) =>
        _icon.ShowBalloonTip(5000, title, text, Forms.ToolTipIcon.None);

    void ShowMenu()
    {
        SetForegroundWindow(_focusSink.Handle);
        _menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }

    static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _focusSink.Dispose();
    }

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hwnd);
}
