using System.Windows;
using System.Windows.Input;
using LookUp.Hotkeys;
using LookUp.UI;

namespace LookUp;

public partial class App : Application
{
    Mutex? _singleInstance;
    GlobalHotkeyService? _hotkeys;
    PopupWindow? _popup;
    SearchWindow? _search;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, @"Local\LookUp.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        _popup = new PopupWindow();
        _search = new SearchWindow();
        _search.LookupRequested += _popup.ShowLookup;

        try
        {
            await _popup.WarmUpAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't start the Microsoft Edge WebView2 runtime.\n\n{ex.Message}", "LookUp");
            Shutdown();
            return;
        }

        // Fixed for now; becomes configurable in Phase 3.
        _hotkeys = new GlobalHotkeyService();
        if (!_hotkeys.Register(ModifierKeys.Control | ModifierKeys.Alt, Key.F, ShowSearch))
            MessageBox.Show("Ctrl+Alt+F is already used by another app.", "LookUp");

        ShowSearch();
    }

    void ShowSearch()
    {
        // Search first: dismissing the popup first would give the foreground away.
        _search!.ShowSearch();
        _popup!.Dismiss();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
