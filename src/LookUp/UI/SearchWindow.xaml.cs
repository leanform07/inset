using System.Windows;
using System.Windows.Input;

namespace LookUp.UI;

public partial class SearchWindow : Window
{
    /// <summary>Raised with the query and the window's top-left corner, so the result can open in the same place.</summary>
    public event Action<string, Point>? LookupRequested;

    public SearchWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowEffects.MakeFloating(this);
        Deactivated += (_, _) => Hide();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public void ShowSearch()
    {
        // Phase 2 moves windows next to the cursor; for now, upper middle of the primary screen.
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + area.Height * 0.22;

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
                    LookupRequested?.Invoke(query, new Point(Left, Top));
                e.Handled = true;
                break;
            case Key.Q when Keyboard.Modifiers == ModifierKeys.Control:
                Application.Current.Shutdown(); // until the tray icon exists (Phase 3)
                break;
        }
    }
}
