using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace LookUp.Selection;

enum SelectionFailure { None, NothingSelected, Elevated, Terminal, KeysHeld }

sealed record SelectionResult(string Text, SelectionFailure Failure, string Method)
{
    public static SelectionResult Fail(SelectionFailure failure) => new("", failure, "");
}

/// <summary>
/// Reads the text selected in the foreground app: UI Automation first (clipboard untouched),
/// then a simulated Ctrl+C with the previous clipboard restored afterwards.
/// </summary>
static class SelectionReader
{
    const int MaxChars = 300;
    static readonly TimeSpan UiaTimeout = TimeSpan.FromMilliseconds(350);

    // Ctrl+C means "interrupt" in these, so the clipboard fallback must not run.
    static readonly HashSet<string> Terminals = new(StringComparer.OrdinalIgnoreCase)
    {
        "WindowsTerminal", "cmd", "powershell", "pwsh", "conhost", "OpenConsole",
        "wezterm-gui", "alacritty", "mintty", "Hyper", "Tabby", "ubuntu", "wsl",
    };

    /// <summary>Must be called on the UI (STA) thread, while the target app is still in the foreground.</summary>
    public static async Task<SelectionResult> ReadAsync()
    {
        var foreground = Native.GetForegroundWindow();
        Native.GetWindowThreadProcessId(foreground, out var pid);

        if (IsElevatedAboveUs(pid)) return SelectionResult.Fail(SelectionFailure.Elevated);

        if (Environment.GetEnvironmentVariable("LOOKUP_SELECTION") != "clipboard") // diagnostic switch
        {
            var uia = Task.Run(ReadWithUiAutomation);
            await Task.WhenAny(uia, Task.Delay(UiaTimeout));
            if (uia.IsCompletedSuccessfully && !string.IsNullOrWhiteSpace(uia.Result))
                return new(uia.Result, SelectionFailure.None, "UI Automation");
        }

        if (Terminals.Contains(ProcessName(pid))) return SelectionResult.Fail(SelectionFailure.Terminal);

        if (!await WaitForModifierReleaseAsync()) return SelectionResult.Fail(SelectionFailure.KeysHeld);
        var copied = await CopyWithClipboardAsync();
        return string.IsNullOrWhiteSpace(copied)
            ? SelectionResult.Fail(SelectionFailure.NothingSelected)
            : new(copied, SelectionFailure.None, "Clipboard");
    }

    // ── UI Automation ─────────────────────────────────────────

    static string? ReadWithUiAutomation()
    {
        try
        {
            // Some apps focus a child of the text control, so look a few levels up.
            var element = AutomationElement.FocusedElement;
            for (int depth = 0; depth < 4 && element != null; depth++)
            {
                if (element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
                {
                    var ranges = ((TextPattern)pattern).GetSelection();
                    return string.Concat(ranges.Select(r => r.GetText(MaxChars)));
                }
                element = TreeWalker.RawViewWalker.GetParent(element);
            }
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException or COMException)
        {
        }
        return null;
    }

    // ── Clipboard fallback ────────────────────────────────────

    /// <summary>The hotkey's Ctrl/Alt are still down when it fires; Ctrl+C sent now would arrive as Ctrl+Alt+C.</summary>
    static async Task<bool> WaitForModifierReleaseAsync()
    {
        int[] modifiers = [Native.VK_CONTROL, Native.VK_MENU, Native.VK_SHIFT, Native.VK_LWIN, Native.VK_RWIN];
        var watch = Stopwatch.StartNew();
        while (modifiers.Any(k => (Native.GetAsyncKeyState(k) & 0x8000) != 0))
        {
            if (watch.ElapsedMilliseconds > 1500) return false;
            await Task.Delay(15);
        }
        return true;
    }

    static async Task<string?> CopyWithClipboardAsync()
    {
        var saved = SnapshotClipboard();
        var before = Native.GetClipboardSequenceNumber();
        SendCtrlC();

        // Wait for the app to put something on the clipboard; no change means nothing was selected.
        var watch = Stopwatch.StartNew();
        while (Native.GetClipboardSequenceNumber() == before)
        {
            if (watch.ElapsedMilliseconds > 600) return null;
            await Task.Delay(15);
        }
        await Task.Delay(30); // some apps add formats in several steps

        var text = Retry(() => Clipboard.ContainsText() ? Clipboard.GetText() : null);
        RestoreClipboard(saved);
        return text?.Length > MaxChars ? text[..MaxChars] : text;
    }

    static void SendCtrlC()
    {
        Native.INPUT Key(int vk, bool up) => new()
        {
            type = Native.INPUT_KEYBOARD,
            ki = new Native.KEYBDINPUT { wVk = (ushort)vk, dwFlags = up ? Native.KEYEVENTF_KEYUP : 0 },
        };
        Native.INPUT[] inputs =
        [
            Key(Native.VK_CONTROL, false), Key(Native.VK_C, false),
            Key(Native.VK_C, true), Key(Native.VK_CONTROL, true),
        ];
        Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Native.INPUT>());
    }

    /// <summary>Copies every format that can be read back; formats an app renders on demand may be lost.</summary>
    static DataObject? SnapshotClipboard()
    {
        var current = Retry(Clipboard.GetDataObject);
        if (current == null) return null;

        var copy = new DataObject();
        foreach (var format in current.GetFormats(autoConvert: false))
        {
            try
            {
                if (current.GetData(format, autoConvert: false) is { } value) copy.SetData(format, value);
            }
            catch (Exception ex) when (ex is COMException or OutOfMemoryException or InvalidOperationException)
            {
            }
        }
        return copy.GetFormats().Length > 0 ? copy : null;
    }

    static void RestoreClipboard(DataObject? saved)
    {
        if (saved == null)
        {
            Retry(() => { Clipboard.Clear(); return true; });
            return;
        }

        // Keep the restored copy out of Win+V history and cloud clipboard; the user never copied it again.
        var hidden = new MemoryStream(new byte[4]);
        saved.SetData("ExcludeClipboardContentFromMonitorProcessing", hidden);
        saved.SetData("CanIncludeInClipboardHistory", new MemoryStream(new byte[4]));
        saved.SetData("CanUploadToCloudClipboard", new MemoryStream(new byte[4]));
        Retry(() => { Clipboard.SetDataObject(saved, copy: true); return true; });
    }

    /// <summary>The clipboard is briefly locked while another app writes to it.</summary>
    static T? Retry<T>(Func<T?> action)
    {
        for (int attempt = 0; ; attempt++)
        {
            try { return action(); }
            catch (COMException) when (attempt < 5) { Thread.Sleep(20); }
            catch (COMException) { return default; }
        }
    }

    // ── Target app checks ─────────────────────────────────────

    static string ProcessName(uint pid)
    {
        try { return Process.GetProcessById((int)pid).ProcessName; }
        catch (ArgumentException) { return ""; }
    }

    /// <summary>Windows blocks input and UI Automation from a normal app into one running as administrator.</summary>
    static bool IsElevatedAboveUs(uint pid) => !IsElevated((uint)Environment.ProcessId) && IsElevated(pid);

    static bool IsElevated(uint pid)
    {
        var process = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == IntPtr.Zero) return false;
        try
        {
            if (!Native.OpenProcessToken(process, Native.TOKEN_QUERY, out var token)) return true; // denied: higher integrity
            try
            {
                return Native.GetTokenInformation(token, Native.TokenElevation, out var elevated, sizeof(int), out _) && elevated != 0;
            }
            finally { Native.CloseHandle(token); }
        }
        finally { Native.CloseHandle(process); }
    }
}
