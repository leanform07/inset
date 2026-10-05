using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using LookUp.Notebook;
using Microsoft.Win32;

namespace LookUp.UI;

/// <summary>Browse, categorise and export saved words.</summary>
public partial class NotebookWindow : Window
{
    enum Kind { All, Uncategorized, Category }

    sealed record CategoryItem(Kind Kind, string Name, string Label, int Count);

    static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    readonly NotebookStore _store;
    readonly ICollectionView _view;

    Kind _kind = Kind.All;
    string _categoryName = "";
    WordNote? _selected;     // the word shown in the detail panel
    bool _loadingDetail;     // suppresses change handlers while the detail panel is filled
    bool _refreshQueued;
    CategoryItem? _menuCategory; // the category that was right-clicked

    /// <summary>Raised with the word and where the popup should open.</summary>
    internal event Action<WordNote, ScreenPlacement.Anchor>? LookupRequested;

    internal NotebookWindow(NotebookStore store)
    {
        _store = store;
        InitializeComponent();

        _view = new CollectionViewSource { Source = store.Words }.View;
        _view.Filter = item => Matches((WordNote)item);
        WordList.ItemsSource = _view;
        CategoryEditor.ItemsSource = store.Categories;

        store.Changed += QueueRefresh;
        Closing += (_, _) => CommitEdits();
        Closed += (_, _) => store.Changed -= QueueRefresh;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                FilterBox.Focus();
                FilterBox.SelectAll();
                e.Handled = true;
            }
        };

        RebuildCategories();
        UpdateCounts();
        ShowDetail();
    }

    // ── Filtering ─────────────────────────────────────────────

    bool Matches(WordNote word)
    {
        if (_kind == Kind.Uncategorized && word.Category.Length > 0) return false;
        if (_kind == Kind.Category && word.Category != _categoryName) return false;
        if (StatusFilterValue() is { } status && word.Status != status) return false;

        var text = FilterBox.Text.Trim();
        return text.Length == 0 ||
               word.Word.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               word.Chinese.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               word.Definition.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               word.Note.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    Familiarity? StatusFilterValue() =>
        FilterNew.IsChecked == true ? Familiarity.New
        : FilterLearning.IsChecked == true ? Familiarity.Learning
        : FilterKnown.IsChecked == true ? Familiarity.Known
        : null;

    void OnFilterChanged(object sender, EventArgs e)
    {
        if (!IsInitialized) return;
        _view.Refresh();
        UpdateCounts();
    }

    /// <summary>Store changes arrive in bursts and sometimes from inside our own handlers; refresh once, later.</summary>
    void QueueRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _refreshQueued = false;
            _view.Refresh();
            RebuildCategories();
            UpdateCounts();
            if (_selected != null)
            {
                LookupInfo.Text = DescribeHistory(_selected);
                UpdateSeal(_selected);
            }
        }, DispatcherPriority.Background);
    }

    void UpdateCounts()
    {
        var shown = _view.Cast<object>().Count();
        CountText.Text = shown == 1 ? "1 WORD" : $"{shown} WORDS";
        EmptyText.Text = _store.Words.Count == 0
            ? "Your notebook is empty.\nLook up a word, then press Ctrl+S to add it here."
            : "No words match.";
        EmptyText.Visibility = shown == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Categories ────────────────────────────────────────────

    void RebuildCategories()
    {
        var items = new List<CategoryItem>
        {
            new(Kind.All, "", "All words", _store.Words.Count),
            new(Kind.Uncategorized, "", "Uncategorized", _store.Words.Count(w => w.Category.Length == 0)),
        };
        items.AddRange(_store.Categories.Select(c =>
            new CategoryItem(Kind.Category, c, c, _store.Words.Count(w => w.Category == c))));

        CategoryList.ItemsSource = items;
        CategoryList.SelectedItem = items.FirstOrDefault(i => i.Kind == _kind && i.Name == _categoryName) ?? items[0];
    }

    void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is not CategoryItem item) return;
        if (item.Kind == _kind && item.Name == _categoryName) return;
        _kind = item.Kind;
        _categoryName = item.Name;
        _view.Refresh();
        UpdateCounts();
    }

    void OnCategoryContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // Only user categories can be renamed or deleted; not "All words" or "Uncategorized".
        _menuCategory = (e.OriginalSource as FrameworkElement)?.DataContext as CategoryItem;
        if (_menuCategory is not { Kind: Kind.Category }) e.Handled = true;
    }

    void OnHowToUseClick(object sender, RoutedEventArgs e) => UserGuide.Open();

    void OnNewCategoryClick(object sender, RoutedEventArgs e)
    {
        if (PromptWindow.Ask(this, "New category", action: "Create") is { } name && !_store.AddCategory(name))
            MessageBox.Show(this, $"There is already a category called “{name}”.", "Notebook");
    }

    void OnRenameCategoryClick(object sender, RoutedEventArgs e)
    {
        if (_menuCategory is not { } item) return;
        if (PromptWindow.Ask(this, "Rename category", item.Name, "Rename") is not { } name) return;

        if (!_store.RenameCategory(item.Name, name))
            MessageBox.Show(this, $"There is already a category called “{name}”.", "Notebook");
        else if (_kind == Kind.Category && _categoryName == item.Name)
            _categoryName = name;
    }

    void OnDeleteCategoryClick(object sender, RoutedEventArgs e)
    {
        if (_menuCategory is not { } item) return;
        var message = item.Count == 0
            ? $"Delete the category “{item.Name}”?"
            : $"Delete the category “{item.Name}”?\nIts {item.Count} word(s) stay in the notebook as Uncategorized.";
        if (MessageBox.Show(this, message, "Notebook", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;

        _store.DeleteCategory(item.Name);
        if (_kind == Kind.Category && _categoryName == item.Name)
        {
            _kind = Kind.All;
            _categoryName = "";
        }
    }

    // ── Word list ─────────────────────────────────────────────

    List<WordNote> SelectedWords() => WordList.SelectedItems.Cast<WordNote>().ToList();

    /// <summary>WORD takes whatever width the fixed columns leave, so nothing scrolls sideways or truncates.</summary>
    void OnWordListSizeChanged(object sender, SizeChangedEventArgs e)
    {
        const double fixedColumns = 116 + 74 + 60, scrollbarAndPadding = 28;
        WordColumn.Width = Math.Max(120, e.NewSize.Width - fixedColumns - scrollbarAndPadding);
    }

    void OnWordSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CommitEdits();
        ShowDetail();
    }

    void OnWordDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_selected != null) RequestLookup(_selected);
    }

    void OnWordListKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete) OnRemoveClick(sender, e);
        else if (e.Key == Key.Enter && _selected != null) RequestLookup(_selected);
    }

    void OnWordMenuOpened(object sender, RoutedEventArgs e)
    {
        var any = WordList.SelectedItems.Count > 0;
        foreach (var item in WordMenu.Items.OfType<MenuItem>()) item.IsEnabled = any;

        MoveToMenu.Items.Clear();
        AddMoveTarget("Uncategorized", "");
        foreach (var category in _store.Categories) AddMoveTarget(category, category);
        MoveToMenu.Items.Add(new Separator());
        var create = new MenuItem { Header = "New category…" };
        create.Click += (_, _) =>
        {
            if (PromptWindow.Ask(this, "New category", action: "Create") is { } name) MoveSelectedTo(name);
        };
        MoveToMenu.Items.Add(create);
    }

    void AddMoveTarget(string header, string category)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => MoveSelectedTo(category);
        MoveToMenu.Items.Add(item);
    }

    void MoveSelectedTo(string category)
    {
        foreach (var word in SelectedWords()) _store.SetCategory(word, category);
        ShowDetail();
    }

    void OnSetStatusClick(object sender, RoutedEventArgs e)
    {
        var status = Enum.Parse<Familiarity>((string)((MenuItem)sender).Tag);
        foreach (var word in SelectedWords()) word.Status = status;
        ShowDetail();
    }

    void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        var words = SelectedWords();
        if (words.Count == 0) return;
        var message = words.Count == 1
            ? $"Remove “{words[0].Word}” from your notebook?"
            : $"Remove {words.Count} words from your notebook?";
        message += "\nTheir lookup counts are kept.";
        if (MessageBox.Show(this, message, "Notebook", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;

        _selected = null; // nothing left to commit
        foreach (var word in words) _store.Remove(word);
    }

    // ── Detail panel ──────────────────────────────────────────

    void ShowDetail()
    {
        var count = WordList.SelectedItems.Count;
        _selected = count == 1 ? (WordNote)WordList.SelectedItem : null;

        NoSelectionText.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        MultiSelectionText.Visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
        MultiSelectionText.Text = $"{count} words selected.\nRight-click to move them to a category\nor set their status.";
        DetailPanel.Visibility = _selected != null ? Visibility.Visible : Visibility.Collapsed;
        if (_selected == null) return;

        _loadingDetail = true;
        DetailPanel.DataContext = _selected;
        DetailPanel.ScrollToTop();
        CategoryEditor.Text = _selected.Category;
        StatusNew.IsChecked = _selected.Status == Familiarity.New;
        StatusLearning.IsChecked = _selected.Status == Familiarity.Learning;
        StatusKnown.IsChecked = _selected.Status == Familiarity.Known;
        ExampleBlock.Visibility = _selected.Example.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        PosText.Text = _selected.PartOfSpeech.ToUpperInvariant();
        LookupInfo.Text = DescribeHistory(_selected);
        UpdateSeal(_selected);
        _loadingDetail = false;
    }

    static string DescribeHistory(WordNote word)
    {
        var text = "ADDED " + word.AddedAt.ToString("yyyy.MM.dd", English);
        if (word.Lookups is { } stat)
            text += $"\nLOOKED UP {stat.Count:00}   LAST {stat.Last.ToString("yyyy.MM.dd", English)}";
        return text;
    }

    /// <summary>The same seal the popup stamps: category and date on the ring, lookup count in the middle.</summary>
    void UpdateSeal(WordNote word)
    {
        var category = word.Category.Length == 0 ? "Uncategorized"
                     : word.Category.Length > 14 ? word.Category[..13] + "…"
                     : word.Category;
        DetailSeal.RingText = $"Notebook · {category} · {word.AddedAt.ToString("yyyy.MM.dd", English)} · ";
        DetailSeal.CenterText = Math.Max(word.LookupCount, 1).ToString("00");
    }

    /// <summary>Saves the note and category being edited; text boxes only commit on focus loss otherwise.</summary>
    void CommitEdits()
    {
        if (_selected == null) return;
        NoteEditor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        CommitCategory(CategoryEditor.Text);
    }

    void CommitCategory(string text)
    {
        if (_loadingDetail || _selected == null || text.Trim() == _selected.Category) return;
        _store.SetCategory(_selected, text);
    }

    void OnCategoryEditorSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 1 && e.AddedItems[0] is string category) CommitCategory(category);
    }

    void OnCategoryEditorLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!CategoryEditor.IsKeyboardFocusWithin) CommitCategory(CategoryEditor.Text);
    }

    void OnCategoryEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CommitCategory(CategoryEditor.Text);
    }

    void OnStatusChecked(object sender, RoutedEventArgs e)
    {
        if (_loadingDetail || _selected == null) return;
        _selected.Status = sender == StatusKnown ? Familiarity.Known
                         : sender == StatusLearning ? Familiarity.Learning
                         : Familiarity.New;
    }

    void OnLookUpAgainClick(object sender, RoutedEventArgs e)
    {
        if (_selected != null) RequestLookup(_selected);
    }

    void OnOpenInCambridgeClick(object sender, RoutedEventArgs e)
    {
        if (_selected != null && Uri.TryCreate(_selected.SourceUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
    }

    void RequestLookup(WordNote word)
    {
        CommitEdits();
        // Over the right-hand side of this window, where the details are.
        var bounds = ScreenPlacement.WindowRect(this);
        var scale = ScreenPlacement.MonitorAt(bounds.TopLeft).Scale;
        var topLeft = new Point(bounds.Right - 460 * scale, bounds.Top + 80 * scale);
        LookupRequested?.Invoke(word, ScreenPlacement.Anchor.TopLeft(topLeft));
    }

    // ── Export ────────────────────────────────────────────────

    void OnExportClick(object sender, RoutedEventArgs e)
    {
        CommitEdits();
        var dialog = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"Inset notebook {DateTime.Now:yyyy-MM-dd}.csv",
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            CsvExport.Write(dialog.FileName, _view.Cast<WordNote>());
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, $"Couldn't save the file. Is it open in Excel?\n\n{ex.Message}", "Export CSV");
        }
    }
}
