using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using LookUp.Settings;

namespace LookUp.Hotkeys;

/// <summary>Registers system-wide hotkeys on a message-only window, one per named action.</summary>
sealed class GlobalHotkeyService : IDisposable
{
    const int WM_HOTKEY = 0x0312;
    const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_WIN = 0x8, MOD_NOREPEAT = 0x4000;

    sealed record Binding(int Id, Hotkey Hotkey, Action Handler);

    readonly HwndSource _source;
    readonly Dictionary<string, Binding> _bindings = [];
    int _nextId = 1;
    bool _suspended;

    public GlobalHotkeyService()
    {
        _source = new HwndSource(new HwndSourceParameters("InsetHotkeys") { ParentWindow = new IntPtr(-3) }); // HWND_MESSAGE
        _source.AddHook(WndProc);
    }

    /// <summary>
    /// Binds the action's hotkey, replacing its previous one.
    /// If Windows refuses (another app owns the combination), the previous hotkey stays and false is returned.
    /// </summary>
    public bool Set(string action, Hotkey hotkey, Action handler)
    {
        _bindings.TryGetValue(action, out var previous);
        if (previous?.Hotkey == hotkey) return true;

        if (previous != null) Unregister(previous);
        var binding = new Binding(_nextId++, hotkey, handler);
        if (_suspended || Register(binding))
        {
            _bindings[action] = binding;
            return true;
        }
        if (previous != null) Register(previous);
        return false;
    }

    /// <summary>Removes the action's hotkey.</summary>
    public void Clear(string action)
    {
        if (!_bindings.Remove(action, out var binding)) return;
        if (!_suspended) Unregister(binding);
    }

    /// <summary>Releases every hotkey, e.g. while the user records a new one in Settings.</summary>
    public void Suspend()
    {
        if (_suspended) return;
        _suspended = true;
        foreach (var binding in _bindings.Values) Unregister(binding);
    }

    public void Resume()
    {
        if (!_suspended) return;
        _suspended = false;
        foreach (var binding in _bindings.Values) Register(binding);
    }

    bool Register(Binding binding)
    {
        var (modifiers, key) = (binding.Hotkey.Modifiers, binding.Hotkey.Key);
        uint mods = MOD_NOREPEAT;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mods |= MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mods |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows)) mods |= MOD_WIN;
        return RegisterHotKey(_source.Handle, binding.Id, mods, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    void Unregister(Binding binding) => UnregisterHotKey(_source.Handle, binding.Id);

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _bindings.Values.FirstOrDefault(b => b.Id == wParam.ToInt32()) is { } binding)
        {
            binding.Handler();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var binding in _bindings.Values) Unregister(binding);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
