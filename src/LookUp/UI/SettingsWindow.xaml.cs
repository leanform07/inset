using System.Windows;
using System.Windows.Controls;
using LookUp.Lookup;
using LookUp.Settings;

namespace LookUp.UI;

/// <summary>Shortcuts, theme, the AI chat and startup. Every change applies and saves immediately.</summary>
public partial class SettingsWindow : Window
{
    readonly App _app;
    readonly AppSettings _settings;
    bool _loading = true;

    internal SettingsWindow(App app, AppSettings settings)
    {
        _app = app;
        _settings = settings;
        InitializeComponent();

        Bind(LookupBox, LookupError, App.LookupSelectionAction, settings.LookupSelection);
        Bind(SearchBox, SearchError, App.SearchAction, settings.Search);
        Bind(NotebookBox, NotebookError, App.NotebookAction, settings.Notebook);

        ThemeSystem.IsChecked = settings.Theme == AppTheme.System;
        ThemeLight.IsChecked = settings.Theme == AppTheme.Light;
        ThemeDark.IsChecked = settings.Theme == AppTheme.Dark;
        AssistantChatGpt.IsChecked = settings.AiAssistant == AiAssistant.ChatGpt;
        AssistantClaude.IsChecked = settings.AiAssistant == AiAssistant.Claude;
        StartupBox.IsChecked = StartupRegistration.IsEnabled;
        _loading = false;
    }

    void Bind(HotkeyBox box, TextBlock error, string action, Hotkey current)
    {
        box.Hotkey = current;
        box.RecordingChanged += recording => _app.PauseHotkeys(recording);
        box.HotkeyPressed += (_, hotkey) => Assign(box, error, action, hotkey);
    }

    void Assign(HotkeyBox box, TextBlock error, string action, Hotkey hotkey)
    {
        var problem = _app.TrySetHotkey(action, hotkey);
        DisplayHotkey(box, error, problem == null ? hotkey : box.Hotkey, problem);
    }

    void OnResetHotkeysClick(object sender, RoutedEventArgs e)
    {
        var problems = _app.ResetHotkeys();
        DisplayHotkey(LookupBox, LookupError, _settings.LookupSelection, problems.GetValueOrDefault(App.LookupSelectionAction));
        DisplayHotkey(SearchBox, SearchError, _settings.Search, problems.GetValueOrDefault(App.SearchAction));
        DisplayHotkey(NotebookBox, NotebookError, _settings.Notebook, problems.GetValueOrDefault(App.NotebookAction));
    }

    static void DisplayHotkey(HotkeyBox box, TextBlock error, Hotkey hotkey, string? problem)
    {
        box.Hotkey = hotkey;
        error.Text = problem ?? "";
        error.Visibility = problem == null ? Visibility.Collapsed : Visibility.Visible;
    }

    void OnThemeChecked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _app.SetTheme(sender == ThemeDark ? AppTheme.Dark : sender == ThemeLight ? AppTheme.Light : AppTheme.System);
    }

    void OnAssistantChecked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _app.SetAssistant(sender == AssistantClaude ? AiAssistant.Claude : AiAssistant.ChatGpt);
    }

    void OnStartupChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        StartupRegistration.Set(StartupBox.IsChecked == true);
    }

    void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
