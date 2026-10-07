using System.IO;
using Microsoft.Win32;

namespace LookUp.Settings;

/// <summary>
/// How many words the notebook had when last saved, kept in HKCU\Software\Inset rather than beside the notebook,
/// so a notebook file that goes missing (folder deleted, moved, replaced) can be noticed instead of silently starting over.
/// </summary>
sealed class NotebookWordCount
{
    public const string DefaultKey = @"Software\Inset";
    const string ValueName = "NotebookWords";

    readonly string _key;
    int? _last;

    public NotebookWordCount(string key = DefaultKey) => _key = key;

    /// <summary>0 if nothing has been recorded.</summary>
    public int Value
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(_key);
            return key?.GetValue(ValueName) is int value ? value : 0;
        }
        set
        {
            if (_last == value) return;
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(_key);
                key.SetValue(ValueName, value, RegistryValueKind.DWord);
                _last = value;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
            {
                // Only a safety net: never let it stop the notebook from saving.
            }
        }
    }
}
