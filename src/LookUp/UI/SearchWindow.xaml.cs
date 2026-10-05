using System.Windows;
using System.Windows.Input;

namespace LookUp.UI;

public partial class SearchWindow : Window
{
    /// <summary>Raised with the query and the window's top-left corner, so the result can open in the same place.</summary>
    internal event Action<string, ScreenPlacement.Anchor>? LookupRequested;

    public SearchWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowEffects.MakeFloating(this);
        Deactivated += (_, _) => Hide();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public void SetShortcuts(string lookupSelection, string notebook) =>
        Hint.Text = $"Enter to search  ·  {lookupSelection} selected text  ·  {notebook} notebook";

    public void ShowSearch()
    {
        // Upper middle of the monitor the cursor is on.
        var monitor = ScreenPlacement.MonitorAt(ScreenPlacement.Cursor());
        var area = monitor.WorkArea;
        var width = Width * monitor.Scale;
        ScreenPlacement.MoveTo(this, new Point(area.Left + (area.Width - width) / 2, area.Top + area.Height * 0.22));

        Show();
        Activate();
        Input.Focus();
        Input.SelectAll();
    }

    void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
        switch (key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
            case Key.Enter:
                var query = Input.Text.Trim();
                // The popup activates first and this window hides when it loses activation.
                // Hiding first would hand the foreground to another app, which then closes the popup.
                if (query.Length > 0)
                    LookupRequested?.Invoke(query, ScreenPlacement.Anchor.TopLeft(ScreenPlacement.WindowRect(this).TopLeft));
                e.Handled = true;
                break;
        }
    }
}
