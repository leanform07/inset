using Microsoft.Win32;

namespace LookUp.Settings;

/// <summary>"Launch at Windows startup", stored where Windows looks for it: HKCU\...\Run.</summary>
static class StartupRegistration
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "Inset";
    const string FormerValueName = "LookUp"; // the placeholder name, until October 2026
    public const string BackgroundArgument = "--background";

    static string Command => $"\"{Environment.ProcessPath}\" {BackgroundArgument}";

    /// <summary>True only if the entry points at this copy of the app.</summary>
    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string value && value == Command;
        }
    }

    /// <summary>A copy named LookUp registered itself under that name; keep the choice, under the new name and path.</summary>
    public static void MoveFromFormerName()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(FormerValueName) == null) return;
        key.DeleteValue(FormerValueName, throwOnMissingValue: false);
        key.SetValue(ValueName, Command);
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(ValueName, Command);
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
