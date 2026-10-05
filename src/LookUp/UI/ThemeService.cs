using System.Windows;
using LookUp.Settings;
using Microsoft.Win32;

namespace LookUp.UI;

/// <summary>
/// Applies Light/Dark/System: the Fluent theme for standard controls, our palette (Themes/*.xaml)
/// for everything else, and — through <see cref="Changed"/> — the dictionary page in the popup.
/// </summary>
static class ThemeService
{
    static AppTheme _setting = AppTheme.System;

    public static bool IsDark { get; private set; }

    /// <summary>Raised after the effective theme changes.</summary>
    public static event Action? Changed;

    static ThemeService()
    {
        // Follow Windows when it switches between light and dark while we are running.
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General && _setting == AppTheme.System)
                Application.Current?.Dispatcher.BeginInvoke(() => Apply(AppTheme.System));
        };
    }

    public static void Apply(AppTheme setting)
    {
        _setting = setting;
        var dark = setting == AppTheme.Dark || (setting == AppTheme.System && WindowsUsesDarkApps());

        var app = Application.Current;
#pragma warning disable WPF0001 // Fluent ThemeMode is marked experimental in .NET 10
        app.ThemeMode = dark ? ThemeMode.Dark : ThemeMode.Light;
#pragma warning restore WPF0001

        var palette = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/LookUp;component/Themes/{(dark ? "Dark" : "Light")}.xaml"),
        };
        // Ours go last: later merged dictionaries win, and the palette overrides some Fluent keys
        // (corner radii, accent and field colours). Shared styles follow the palette they read.
        var dictionaries = app.Resources.MergedDictionaries;
        var current = dictionaries.FirstOrDefault(IsPalette);
        var shared = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.EndsWith("Themes/Shared.xaml", StringComparison.Ordinal) == true);
        if (current != null) dictionaries.Remove(current);
        if (shared != null) dictionaries.Remove(shared);
        dictionaries.Add(palette);
        if (shared != null) dictionaries.Add(shared);

        if (dark == IsDark && current != null) return;
        IsDark = dark;
        Changed?.Invoke();
    }

    /// <summary>Our Themes/Light.xaml or Themes/Dark.xaml (not Fluent's own Fluent.Light.xaml).</summary>
    static bool IsPalette(ResourceDictionary dictionary) =>
        dictionary.Source?.OriginalString is { } source &&
        (source.EndsWith("Themes/Light.xaml", StringComparison.Ordinal) || source.EndsWith("Themes/Dark.xaml", StringComparison.Ordinal)) &&
        !source.Contains("Fluent", StringComparison.Ordinal);

    static bool WindowsUsesDarkApps()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
    }
}
