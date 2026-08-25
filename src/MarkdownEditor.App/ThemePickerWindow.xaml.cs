using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarkdownEditor.Core.Theming;

namespace MarkdownEditor.App;

public partial class ThemePickerWindow : Window
{
    private readonly string _activeThemeId;
    private string _filter = "all";

    public ThemePickerWindow(string activeThemeId)
    {
        _activeThemeId = activeThemeId;
        InitializeComponent();
        DataContext = this;
        RefreshThemes();
        Loaded += (_, _) => SearchBox.Focus();
    }

    public ObservableCollection<ThemePickerItem> VisibleThemes { get; } = [];

    public string? SelectedThemeId { get; private set; }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        SearchHint.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
        RefreshThemes();
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        _filter = (sender as System.Windows.Controls.RadioButton)?.Tag as string ?? "all";
        RefreshThemes();
    }

    private void ThemeCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { DataContext: ThemePickerItem item })
        {
            return;
        }

        SelectedThemeId = item.Theme.Id;
        DialogResult = true;
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        DialogResult = false;
        e.Handled = true;
    }

    private void RefreshThemes()
    {
        var search = SearchBox?.Text?.Trim() ?? string.Empty;
        var themes = ThemeCatalog.Search(search, FilterValue());

        VisibleThemes.Clear();
        foreach (var theme in themes)
        {
            VisibleThemes.Add(new ThemePickerItem(
                theme,
                string.Equals(theme.Id, _activeThemeId, StringComparison.OrdinalIgnoreCase)));
        }

        if (ResultCountText is null)
        {
            return;
        }

        ResultCountText.Text = VisibleThemes.Count == 1 ? "1 palette" : $"{VisibleThemes.Count} palettes";
        NoResultsPanel.Visibility = VisibleThemes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CatalogSummaryText.Text = $"{ThemeCatalog.All.Count} BUILT-INS";
    }

    private bool? FilterValue() => _filter switch
    {
        "light" => false,
        "dark" => true,
        _ => null,
    };
}

public sealed record ThemePickerItem(ThemeDefinition Theme, bool IsSelected)
{
    public string ModeLabel => Theme.IsDark ? "DARK" : "LIGHT";
}
