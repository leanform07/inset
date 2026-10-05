using System.Windows.Input;
using LookUp.Settings;

namespace LookUp.Tests;

public sealed class HotkeyTests
{
    [Theory]
    [InlineData("Ctrl+Alt+D", ModifierKeys.Control | ModifierKeys.Alt, Key.D)]
    [InlineData("ctrl + shift + f", ModifierKeys.Control | ModifierKeys.Shift, Key.F)]
    [InlineData("Win+Alt+1", ModifierKeys.Windows | ModifierKeys.Alt, Key.D1)]
    [InlineData("Ctrl+F12", ModifierKeys.Control, Key.F12)]
    [InlineData("Alt+/", ModifierKeys.Alt, Key.OemQuestion)]
    public void Parses_shortcuts(string text, ModifierKeys modifiers, Key key)
    {
        Assert.True(Hotkey.TryParse(text, out var hotkey));
        Assert.Equal(new Hotkey(modifiers, key), hotkey);
    }

    [Theory]
    [InlineData("D")]                // no modifier
    [InlineData("Shift+D")]          // Shift alone would block normal typing
    [InlineData("Ctrl+Alt")]         // no key
    [InlineData("Ctrl+D+F")]         // two keys
    [InlineData("Ctrl+Banana")]
    [InlineData("")]
    public void Rejects_unusable_shortcuts(string text) =>
        Assert.False(Hotkey.TryParse(text, out _));

    [Fact]
    public void Round_trips_through_text()
    {
        foreach (var hotkey in new[]
                 {
                     new Hotkey(ModifierKeys.Control | ModifierKeys.Alt, Key.D),
                     new Hotkey(ModifierKeys.Windows | ModifierKeys.Shift, Key.D7),
                     new Hotkey(ModifierKeys.Control, Key.OemComma),
                 })
        {
            Assert.True(Hotkey.TryParse(hotkey.ToString(), out var parsed), hotkey.ToString());
            Assert.Equal(hotkey, parsed);
        }
    }

    [Fact]
    public void Formats_in_a_fixed_modifier_order()
    {
        Assert.Equal("Ctrl+Alt+Shift+Win+K",
            new Hotkey(ModifierKeys.Windows | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Control, Key.K).ToString());
    }

    [Fact]
    public void Settings_fall_back_to_defaults_when_text_is_unusable()
    {
        var settings = new AppSettings { LookupSelectionHotkey = "nonsense" };
        Assert.Equal(AppSettings.DefaultLookupSelection, settings.LookupSelection);
    }
}
