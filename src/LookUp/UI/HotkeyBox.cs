using System.Windows.Controls;
using System.Windows.Input;
using LookUp.Settings;

namespace LookUp.UI;

/// <summary>A box that records a shortcut: click it, then press the keys.</summary>
sealed class HotkeyBox : TextBox
{
    Hotkey _hotkey;

    /// <summary>Raised with a complete shortcut; set <see cref="Hotkey"/> to accept it.</summary>
    public event Action<HotkeyBox, Hotkey>? HotkeyPressed;

    /// <summary>Raised when recording starts and stops, so global hotkeys can be paused meanwhile.</summary>
    public event Action<bool>? RecordingChanged;

    public HotkeyBox()
    {
        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;
        IsUndoEnabled = false;
        Cursor = Cursors.Hand;
        InputMethod.SetIsInputMethodEnabled(this, false);
        SetResourceReference(StyleProperty, typeof(TextBox)); // implicit TextBox styles skip subclasses
    }

    public Hotkey Hotkey
    {
        get => _hotkey;
        set
        {
            _hotkey = value;
            Text = IsKeyboardFocused ? $"{value}  ✓" : value.ToString();
        }
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        Text = "Press a shortcut…";
        RecordingChanged?.Invoke(true);
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        Text = _hotkey.ToString();
        RecordingChanged?.Invoke(false);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var key = e.Key switch
        {
            Key.System => e.SystemKey,          // keys pressed with Alt
            Key.ImeProcessed => e.ImeProcessedKey,
            _ => e.Key,
        };
        var modifiers = Keyboard.Modifiers;

        if (modifiers == ModifierKeys.None && key is Key.Tab) return; // let Tab move on
        e.Handled = true;

        if (modifiers == ModifierKeys.None && key is Key.Escape)
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            return;
        }

        var candidate = new Hotkey(modifiers, key);
        if (candidate.IsValid) HotkeyPressed?.Invoke(this, candidate);
        else Text = modifiers == ModifierKeys.None ? "Use Ctrl, Alt or Win with a key" : Hotkey.ModifierText(modifiers) + "…";
    }
}
