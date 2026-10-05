using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace LookUp.Settings;

enum AppTheme { System, Light, Dark }

/// <summary>A global shortcut such as Ctrl+Alt+D. Needs at least Ctrl, Alt or Win.</summary>
readonly record struct Hotkey(ModifierKeys Modifiers, Key Key)
{
    public bool IsValid =>
        (Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0 &&
        Key is not (Key.None or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
                    Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System);

    public override string ToString() => ModifierText(Modifiers) + KeyName(Key);

    /// <summary>"Ctrl+Alt+" for the held modifiers; empty when there are none.</summary>
    public static string ModifierText(ModifierKeys modifiers)
    {
        var text = "";
        if (modifiers.HasFlag(ModifierKeys.Control)) text += "Ctrl+";
        if (modifiers.HasFlag(ModifierKeys.Alt)) text += "Alt+";
        if (modifiers.HasFlag(ModifierKeys.Shift)) text += "Shift+";
        if (modifiers.HasFlag(ModifierKeys.Windows)) text += "Win+";
        return text;
    }

    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var modifiers = ModifierKeys.None;
        var key = Key.None;
        foreach (var part in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= ModifierKeys.Control; break;
                case "alt": modifiers |= ModifierKeys.Alt; break;
                case "shift": modifiers |= ModifierKeys.Shift; break;
                case "win" or "windows": modifiers |= ModifierKeys.Windows; break;
                default:
                    if (key != Key.None || !TryParseKey(part, out key)) return false;
                    break;
            }
        }
        hotkey = new Hotkey(modifiers, key);
        return hotkey.IsValid;
    }

    static string KeyName(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => ((int)(key - Key.D0)).ToString(),
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemQuestion => "/",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemMinus => "-",
        Key.OemPlus => "=",
        Key.Oem3 => "`",
        _ => key.ToString(),
    };

    static bool TryParseKey(string text, out Key key)
    {
        foreach (var candidate in Enum.GetValues<Key>())
        {
            if (KeyName(candidate).Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                key = candidate;
                return true;
            }
        }
        return Enum.TryParse(text, ignoreCase: true, out key);
    }
}

/// <summary>User preferences in %AppData%\LookUp\settings.json. Launch-at-startup lives in the registry instead.</summary>
sealed class AppSettings
{
    public static readonly Hotkey DefaultLookupSelection = new(ModifierKeys.Control | ModifierKeys.Alt, Key.D);
    public static readonly Hotkey DefaultSearch = new(ModifierKeys.Control | ModifierKeys.Alt, Key.F);
    public static readonly Hotkey DefaultNotebook = new(ModifierKeys.Control | ModifierKeys.Alt, Key.N);

    public string LookupSelectionHotkey { get; set; } = DefaultLookupSelection.ToString();
    public string SearchHotkey { get; set; } = DefaultSearch.ToString();
    public string NotebookHotkey { get; set; } = DefaultNotebook.ToString();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>The tray balloon explaining the hotkeys is shown on first run only.</summary>
    public bool WelcomeShown { get; set; }

    /// <summary>The result window's size in DIPs once the user has resized it; absent means the default.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? PopupWidth { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? PopupHeight { get; set; }

    [JsonIgnore] public System.Windows.Size? PopupSize
    {
        get => PopupWidth is > 0 && PopupHeight is > 0 ? new(PopupWidth.Value, PopupHeight.Value) : null;
        set
        {
            PopupWidth = value?.Width;
            PopupHeight = value?.Height;
        }
    }

    [JsonIgnore] public Hotkey LookupSelection => Parse(LookupSelectionHotkey, DefaultLookupSelection);
    [JsonIgnore] public Hotkey Search => Parse(SearchHotkey, DefaultSearch);
    [JsonIgnore] public Hotkey Notebook => Parse(NotebookHotkey, DefaultNotebook);

    static Hotkey Parse(string text, Hotkey fallback) => Hotkey.TryParse(text, out var hotkey) ? hotkey : fallback;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep "Ctrl+Alt+D" readable
    };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LookUp", "settings.json");

    public static AppSettings Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new()
                : new();
        }
        catch (JsonException)
        {
            return new(); // unreadable settings fall back to defaults; they are rewritten on the next save
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
