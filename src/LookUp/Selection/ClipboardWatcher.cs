using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace LookUp.Selection;

/// <summary>
/// Hands copied text to a callback while started. Started only while an AI conclusion is expected,
/// so Inset does not look at the clipboard the rest of the time.
/// </summary>
sealed class ClipboardWatcher : IDisposable
{
    const int WM_CLIPBOARDUPDATE = 0x031D;

    readonly Action<string> _onText;
    readonly HwndSource _source;
    bool _listening;

    public ClipboardWatcher(Action<string> onText)
    {
        _onText = onText;
        _source = new HwndSource(new HwndSourceParameters("InsetClipboard") { ParentWindow = new IntPtr(-3) }); // HWND_MESSAGE
        _source.AddHook(WndProc);
    }

    public void Start()
    {
        if (!_listening) _listening = AddClipboardFormatListener(_source.Handle);
    }

    public void Stop()
    {
        if (_listening) RemoveClipboardFormatListener(_source.Handle);
        _listening = false;
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Read after the message: the app that copied may still hold the clipboard open.
        if (msg == WM_CLIPBOARDUPDATE) _source.Dispatcher.BeginInvoke(ReadText, DispatcherPriority.Background);
        return IntPtr.Zero;
    }

    async void ReadText()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Clipboard.ContainsText()) _onText(Clipboard.GetText());
                return;
            }
            catch (COMException)
            {
                await Task.Delay(50); // another app has the clipboard open
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)] static extern bool AddClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
}
