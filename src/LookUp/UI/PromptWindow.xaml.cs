using System.Windows;

namespace LookUp.UI;

/// <summary>A one-line text prompt, used for category names.</summary>
public partial class PromptWindow : Window
{
    internal PromptWindow() => InitializeComponent(); // use Ask(); internal so tests can render it

    /// <returns>The trimmed text, or null if cancelled or left blank.</returns>
    public static string? Ask(Window owner, string title, string initial = "", string action = "Save")
    {
        var prompt = new PromptWindow { Owner = owner, Title = title };
        prompt.Input.Text = initial;
        prompt.ActionButton.Content = action;
        prompt.Loaded += (_, _) =>
        {
            prompt.Input.Focus();
            prompt.Input.SelectAll();
        };
        var text = prompt.ShowDialog() == true ? prompt.Input.Text.Trim() : "";
        return text.Length > 0 ? text : null;
    }

    void OnOkClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
