using Microsoft.Win32;

namespace LookUp.Settings;

/// <summary>"Launch at Windows startup", stored where Windows looks for it: HKCU\...\Run.</summary>
static class StartupRegistration
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "LookUp";
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

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(ValueName, Command);
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
