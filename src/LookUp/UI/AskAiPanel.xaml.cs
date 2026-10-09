using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LookUp.Lookup;

namespace LookUp.UI;

/// <summary>
/// "Ask AI" under an entry, in the popup and the notebook: a follow-up question about the word,
/// opened in the user's own ChatGPT or Claude in the browser. A shortcut chooses the question and
/// Ask sends it; the field holds the words for Compare…, an optional note for the other shortcuts,
/// or, with none chosen, a question of its own.
/// </summary>
public partial class AskAiPanel : UserControl
{
    string _word = "", _partOfSpeech = "", _chinese = "", _definition = "";
    AiAssistant _assistant;
    AskAi.Kind? _shortcut; // the shortcut chosen, if any

    /// <summary>Raised when the panel closes, after asking or cancelling, so the host can take focus back.</summary>
    internal event Action? Finished;

    /// <summary>Raised with the word when the question is sent or copied: its conclusion may come back.</summary>
    internal event Action<string>? Asked;

    public AskAiPanel() => InitializeComponent();

    internal AiAssistant Assistant
    {
        get => _assistant;
        set
        {
            _assistant = value;
            Heading.Text = $"ASK {AskAi.Name(value).ToUpperInvariant()}";
            AskLabel.Text = $"Ask {AskAi.Name(value)}";
        }
    }

    internal bool IsOpen => Visibility == Visibility.Visible;

    internal void Open(string word, string partOfSpeech, string chinese, string definition)
    {
        (_word, _partOfSpeech, _chinese, _definition) = (word, partOfSpeech, chinese, definition);
        Assistant = _assistant;
        Input.Text = "";
        Choose(null);
        Visibility = Visibility.Visible;
        Input.Focus();
    }

    internal void Close() => Visibility = Visibility.Collapsed;

    /// <summary>What Ask (or Enter) sends: the chosen shortcut, otherwise whatever the field holds.</summary>
    AskAi.Kind CurrentKind => _shortcut ?? AskAi.Classify(Input.Text);

    bool CanAsk => CurrentKind != AskAi.Kind.Compare || AskAi.OtherWords(Input.Text).Count > 0;

    string Prompt() => AskAi.Prompt(_word, _partOfSpeech, _chinese, _definition, CurrentKind, Input.Text);

    /// <summary>Marks the chosen shortcut (null for none) and says what the field is for.</summary>
    void Choose(AskAi.Kind? shortcut)
    {
        _shortcut = shortcut;
        foreach (var button in Shortcuts.Children.OfType<Button>())
            button.Style = (Style)FindResource(Enum.Parse<AskAi.Kind>((string)button.Tag) == shortcut ? "ShortcutOn" : "Shortcut");
        Placeholder.Text = shortcut switch
        {
            null => "Or type your own question",
            AskAi.Kind.Compare => "Words to compare, separated by commas",
            _ => "Anything to add (optional)",
        };
        UpdateHint();
    }

    void UpdateHint()
    {
        CopyButton.Content = "COPY QUESTION";
        AskButton.IsEnabled = CopyButton.IsEnabled = CanAsk;
        var hint = CurrentKind switch
        {
            AskAi.Kind.Compare when !CanAsk => "Type one or more words to compare it with.",
            AskAi.Kind.Compare => $"Asks how “{_word}” differs from {List(AskAi.OtherWords(Input.Text))}.",
            AskAi.Kind.Usage => $"Asks for more on how “{_word}” is used.",
            AskAi.Kind.Collocations => $"Asks which words “{_word}” is usually used with.",
            AskAi.Kind.Memory => $"Asks for a way to remember “{_word}”.",
            _ => $"Asks your question about “{_word}”.",
        };
        var note = _shortcut is AskAi.Kind.Usage or AskAi.Kind.Collocations or AskAi.Kind.Memory && Input.Text.Trim().Length > 0;
        Hint.Text = note ? hint + " Adds what you typed." : hint;
    }

    /// <summary>“effect”, “influence” and “impact”.</summary>
    static string List(IReadOnlyList<string> words)
    {
        var quoted = words.Select(w => $"“{w}”").ToList();
        return quoted.Count == 1 ? quoted[0] : string.Join(", ", quoted[..^1]) + " and " + quoted[^1];
    }

    void OnInputChanged(object sender, TextChangedEventArgs e) => UpdateHint();

    void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        // ImeProcessed keys belong to the Chinese IME (Enter picks a candidate, Esc drops it).
        if (e.Key == Key.Enter) OnAskClick(sender, e);
        else if (e.Key == Key.Escape) OnCancelClick(sender, e);
        else return;
        e.Handled = true;
    }

    /// <summary>Chooses the shortcut, or clears it when it is chosen already. Ask or Enter sends.</summary>
    void OnShortcutClick(object sender, RoutedEventArgs e)
    {
        var kind = Enum.Parse<AskAi.Kind>((string)((Button)sender).Tag);
        Choose(_shortcut == kind ? null : kind);
        Input.Focus();
        Input.CaretIndex = Input.Text.Length;
    }

    void OnAskClick(object sender, RoutedEventArgs e)
    {
        if (!CanAsk) return;
        var url = AskAi.ChatUrl(_assistant, Prompt());
        Asked?.Invoke(_word);
        Close();
        Finished?.Invoke();
        Process.Start(new ProcessStartInfo(url.ToString()) { UseShellExecute = true });
    }

    void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
        Finished?.Invoke();
    }

    void OnCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(Prompt());
            CopyButton.Content = "COPIED";
            Asked?.Invoke(_word);
        }
        catch (COMException)
        {
            CopyButton.Content = "COULDN'T COPY · TRY AGAIN"; // another app is holding the clipboard
        }
        Input.Focus();
    }
}
