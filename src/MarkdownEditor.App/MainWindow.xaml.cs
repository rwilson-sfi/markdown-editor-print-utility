using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MarkdownEditor.Core.Documents;
using MarkdownEditor.Core.Recovery;
using MarkdownEditor.Core.Theming;
using Microsoft.Web.WebView2.Core;
using ContextMenu = System.Windows.Controls.ContextMenu;
using DataFormats = System.Windows.DataFormats;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using OpenFolderDialog = Microsoft.Win32.OpenFolderDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WinForms = System.Windows.Forms;

namespace MarkdownEditor.App;

public partial class MainWindow : Window, IDisposable
{
    private static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MarkdownEditor");

    private readonly DocumentFileService _fileService = new();
    private readonly MarkdownRenderer _renderer = new();
    private readonly IReadOnlyList<string> _initialPaths;
    private readonly WinForms.NotifyIcon _trayIcon;
    private readonly ThemeManager _themeManager = new();
    private readonly RecentFilesStore _recentFiles = new(Path.Combine(AppDataDirectory, "recent-files.json"));
    private readonly RecoveryDraftStore _recoveryStore = new(Path.Combine(AppDataDirectory, "RecoveryDrafts"));
    private readonly DispatcherTimer _recoveryTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly Dictionary<string, string> _draftedMarkdown = [];
    private readonly Dictionary<string, DateTimeOffset> _dismissedDiskChanges = [];
    private readonly HashSet<string> _notifiedMissingOnDisk = [];
    private ThemeDefinition _activeTheme;
    private DocumentSession? _activeDocument;
    private bool _editorReady;
    private bool _allowClose;
    private bool _checkingExternalChanges;
    private bool _disposed;

    public MainWindow(IReadOnlyList<string>? initialPaths = null)
    {
        _initialPaths = initialPaths ?? [];
        _activeTheme = _themeManager.ApplySavedTheme();
        InitializeComponent();
        DataContext = this;
        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Markdown Editor",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu(),
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(RestoreWindow);
        _recoveryTimer.Tick += async (_, _) => await SnapshotDirtyDocumentsAsync();
        UpdateThemeVisualState();
        UpdateWindowState();
    }

    public ObservableCollection<DocumentSession> Documents { get; } = [];

    public ObservableCollection<FolderTreeNode> FolderRoots { get; } = [];

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeEditorAsync();
        await RestoreRecoveryDraftsAsync();
        if (_initialPaths.Count > 0)
        {
            await OpenPathsAsync(_initialPaths);
        }

        if (Documents.Count == 0)
        {
            CreateUntitledDocument();
        }

        _recoveryTimer.Start();
    }

    private async void Window_Activated(object? sender, EventArgs e) => await CheckExternalChangesAsync();

    public async Task OpenPathsFromExternalAsync(IReadOnlyList<string> paths)
    {
        RestoreWindow();
        await OpenPathsAsync(paths);
    }

    private async Task InitializeEditorAsync()
    {
        var assetDirectory = Path.Combine(AppContext.BaseDirectory, "EditorAssets");
        var entryPoint = Path.Combine(assetDirectory, "index.html");
        if (!File.Exists(entryPoint))
        {
            StatusText.Text = "Editor assets are missing. Rebuild the application.";
            return;
        }

        try
        {
            _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
            await EditorWebView.EnsureCoreWebView2Async();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            StatusText.Text = "Microsoft Edge WebView2 Runtime is required.";
            MessageBox.Show(this, "Microsoft Edge WebView2 Runtime is required to use the formatted editor.",
                "WebView2 Runtime not found", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        EditorWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        EditorWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        EditorWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        EditorWebView.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
        EditorWebView.CoreWebView2.WebMessageReceived += Editor_WebMessageReceived;
        EditorWebView.CoreWebView2.NavigationStarting += (_, args) =>
        {
            if (!args.Uri.StartsWith("https://appassets.local/", StringComparison.OrdinalIgnoreCase))
            {
                args.Cancel = true;
            }
        };
        EditorWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets.local", assetDirectory, CoreWebView2HostResourceAccessKind.DenyCors);
        EditorWebView.Source = new Uri("https://appassets.local/index.html");
        StatusText.Text = "Loading editor…";
    }

    private async void Editor_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var message = JsonDocument.Parse(e.WebMessageAsJson);
        var root = message.RootElement;
        if (!root.TryGetProperty("type", out var typeProperty))
        {
            return;
        }

        switch (typeProperty.GetString())
        {
            case "ready":
                _editorReady = true;
                await SendThemeToEditorAsync();
                await LoadActiveDocumentAsync();
                break;
            case "changed" when root.TryGetProperty("markdown", out var markdownProperty):
                var documentId = root.TryGetProperty("documentId", out var idProperty)
                    ? idProperty.GetString()
                    : null;
                var document = Documents.FirstOrDefault(candidate => candidate.Id == documentId) ?? _activeDocument;
                document?.SetMarkdown(markdownProperty.GetString() ?? string.Empty);
                if (ReferenceEquals(document, _activeDocument))
                {
                    UpdateWindowState();
                }

                break;
        }
    }

    private Task SendToEditorAsync(object message)
    {
        if (_editorReady && EditorWebView.CoreWebView2 is not null)
        {
            EditorWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message));
        }

        return Task.CompletedTask;
    }

    private Task SendThemeToEditorAsync() => SendToEditorAsync(new
    {
        type = "theme",
        theme = new
        {
            id = _activeTheme.Id,
            isDark = _activeTheme.IsDark,
            monospaceInterface = _activeTheme.MonospaceInterface,
            background = _activeTheme.Background,
            surface = _activeTheme.Card,
            muted = _activeTheme.Muted,
            foreground = _activeTheme.Foreground,
            mutedForeground = _activeTheme.MutedForeground,
            border = _activeTheme.Border,
            accent = _activeTheme.Ring,
            accentForeground = _activeTheme.RingForeground,
            selection = _activeTheme.Selection,
            sidebar = _activeTheme.SidebarBackground,
            code = _activeTheme.Muted,
            destructive = _activeTheme.Destructive,
        },
    });

    private void New_Click(object sender, RoutedEventArgs e) => CreateUntitledDocument();

    private void CreateUntitledDocument()
    {
        var document = DocumentSession.CreateUntitled();
        Documents.Add(document);
        DocumentTabs.SelectedItem = document;
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Markdown documents",
            Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true,
        };
        if (dialog.ShowDialog(this) == true)
        {
            await OpenPathsAsync(dialog.FileNames);
        }
    }

    private async Task OpenPathsAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(MarkdownPath.IsSupported).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await OpenPathAsync(path);
        }
    }

    private async Task OpenPathAsync(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var existing = Documents.FirstOrDefault(document =>
            string.Equals(document.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            DocumentTabs.SelectedItem = existing;
            return;
        }

        try
        {
            var file = await _fileService.LoadAsync(fullPath);
            var document = DocumentSession.FromFile(file);
            Documents.Add(document);
            DocumentTabs.SelectedItem = document;
            _recentFiles.Touch(fullPath);
            if (FolderRoots.Count == 0 && Path.GetDirectoryName(fullPath) is { } folder)
            {
                SetFolderRoot(folder);
            }
        }
        catch (UnsupportedTextEncodingException exception)
        {
            MessageBox.Show(this, exception.Message, "Unsupported encoding", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, exception.Message, "Unable to open document", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RestoreRecoveryDraftsAsync()
    {
        IReadOnlyList<RecoveryDraft> drafts;
        try
        {
            drafts = _recoveryStore.LoadDrafts();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        if (drafts.Count == 0)
        {
            return;
        }

        var result = MessageBox.Show(this,
            $"A previous session left {drafts.Count} unsaved document(s) behind.\n\nRestore them now? Choosing No deletes the recovered drafts.",
            "Recovered documents", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            TryClearRecoveryStore();
            return;
        }

        foreach (var draft in drafts)
        {
            var document = DocumentSession.FromRecoveredDraft(
                draft.OriginalPath, draft.Markdown, draft.HasUtf8Bom, draft.LineEnding);
            Documents.Add(document);
            DocumentTabs.SelectedItem = document;
        }

        // Restored sessions carry new IDs, so re-snapshot under those IDs before dropping the old drafts.
        TryClearRecoveryStore();
        await SnapshotDirtyDocumentsAsync();
        StatusText.Text = $"Restored {drafts.Count} recovered document(s). Save them to keep the content.";
    }

    private async Task SnapshotDirtyDocumentsAsync()
    {
        foreach (var document in Documents.ToArray())
        {
            try
            {
                if (document.IsDirty)
                {
                    if (_draftedMarkdown.TryGetValue(document.Id, out var drafted)
                        && string.Equals(drafted, document.Markdown, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    await _recoveryStore.SaveDraftAsync(new RecoveryDraft(
                        document.Id,
                        document.FilePath,
                        document.Markdown,
                        document.HasUtf8Bom,
                        document.LineEnding,
                        DateTimeOffset.UtcNow));
                    _draftedMarkdown[document.Id] = document.Markdown;
                }
                else if (_draftedMarkdown.Remove(document.Id))
                {
                    _recoveryStore.DeleteDraft(document.Id);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Draft persistence is best-effort; editing continues with in-memory content.
            }
        }
    }

    private void DiscardDraft(DocumentSession document)
    {
        _draftedMarkdown.Remove(document.Id);
        _dismissedDiskChanges.Remove(document.Id);
        _notifiedMissingOnDisk.Remove(document.Id);
        try
        {
            _recoveryStore.DeleteDraft(document.Id);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A stale draft is re-offered on the next start and can be declined there.
        }
    }

    private void TryClearRecoveryStore()
    {
        try
        {
            _recoveryStore.Clear();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Leftover drafts are re-offered on the next start.
        }
    }

    private async Task CheckExternalChangesAsync()
    {
        if (_checkingExternalChanges)
        {
            return;
        }

        _checkingExternalChanges = true;
        try
        {
            foreach (var document in Documents.ToArray())
            {
                if (document.FilePath is not { } path)
                {
                    continue;
                }

                var exists = File.Exists(path);
                DateTimeOffset? currentWriteTimeUtc = exists ? File.GetLastWriteTimeUtc(path) : null;
                switch (ExternalChangeInspector.Classify(exists, document.LastWriteTimeUtc, currentWriteTimeUtc))
                {
                    case ExternalDocumentState.Unchanged:
                        _notifiedMissingOnDisk.Remove(document.Id);
                        break;
                    case ExternalDocumentState.MissingOnDisk:
                        if (_notifiedMissingOnDisk.Add(document.Id))
                        {
                            document.MarkMissingOnDisk();
                            if (ReferenceEquals(document, _activeDocument))
                            {
                                UpdateWindowState();
                            }

                            StatusText.Text =
                                $"{document.DisplayName} was deleted or moved on disk. Saving will recreate it.";
                        }

                        break;
                    case ExternalDocumentState.ChangedOnDisk:
                        if (_dismissedDiskChanges.TryGetValue(document.Id, out var dismissed)
                            && dismissed == currentWriteTimeUtc)
                        {
                            break;
                        }

                        if (!document.IsDirty)
                        {
                            await ReloadDocumentAsync(document);
                        }
                        else
                        {
                            var choice = MessageBox.Show(this,
                                $"{document.DisplayName} changed on disk and has unsaved changes in this editor.\n\nReload from disk and discard the unsaved changes?",
                                "File changed on disk", MessageBoxButton.YesNo, MessageBoxImage.Warning,
                                MessageBoxResult.No);
                            if (choice == MessageBoxResult.Yes)
                            {
                                await ReloadDocumentAsync(document);
                            }
                            else if (currentWriteTimeUtc is { } keptTimestamp)
                            {
                                _dismissedDiskChanges[document.Id] = keptTimestamp;
                            }
                        }

                        break;
                }
            }
        }
        finally
        {
            _checkingExternalChanges = false;
        }
    }

    private async Task ReloadDocumentAsync(DocumentSession document)
    {
        if (document.FilePath is not { } path)
        {
            return;
        }

        try
        {
            var file = await _fileService.LoadAsync(path);
            document.ReloadFromFile(file);
            DiscardDraft(document);
            if (ReferenceEquals(document, _activeDocument))
            {
                await LoadActiveDocumentAsync();
            }

            StatusText.Text = $"Reloaded {document.DisplayName} after an external change.";
        }
        catch (UnsupportedTextEncodingException exception)
        {
            MessageBox.Show(this, exception.Message, "Unsupported encoding", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, exception.Message, "Unable to reload document", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != DocumentTabs)
        {
            return;
        }

        _activeDocument = DocumentTabs.SelectedItem as DocumentSession;
        await LoadActiveDocumentAsync();
    }

    private async Task LoadActiveDocumentAsync()
    {
        if (_activeDocument is null)
        {
            UpdateWindowState();
            return;
        }

        await SendToEditorAsync(new
        {
            type = "load",
            documentId = _activeDocument.Id,
            markdown = _activeDocument.Markdown,
        });
        await SendToEditorAsync(new { type = "mode", mode = _activeDocument.EditorMode });
        UpdateWindowState();
    }

    private async void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: DocumentSession document }
            || !await ConfirmCloseDocumentAsync(document))
        {
            return;
        }

        Documents.Remove(document);
        DiscardDraft(document);
        if (Documents.Count == 0)
        {
            CreateUntitledDocument();
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_activeDocument is not null)
        {
            await SaveDocumentAsync(_activeDocument, forceSaveAs: false);
        }
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (_activeDocument is not null)
        {
            await SaveDocumentAsync(_activeDocument, forceSaveAs: true);
        }
    }

    private async Task<bool> SaveDocumentAsync(DocumentSession document, bool forceSaveAs)
    {
        var targetPath = document.FilePath;
        if (forceSaveAs || targetPath is null)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Markdown document",
                Filter = "Markdown file (*.md)|*.md|Markdown file (*.markdown)|*.markdown",
                DefaultExt = ".md",
                AddExtension = true,
                FileName = targetPath is null ? "Untitled.md" : Path.GetFileName(targetPath),
            };
            if (dialog.ShowDialog(this) != true)
            {
                return false;
            }

            targetPath = dialog.FileName;
            var duplicate = Documents.FirstOrDefault(candidate =>
                !ReferenceEquals(candidate, document)
                && string.Equals(candidate.FilePath, Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                DocumentTabs.SelectedItem = duplicate;
                MessageBox.Show(this, "That file is already open in another tab.", "File already open",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }

        var fullTargetPath = Path.GetFullPath(targetPath);
        if (string.Equals(fullTargetPath, document.FilePath, StringComparison.OrdinalIgnoreCase)
            && File.Exists(fullTargetPath)
            && ExternalChangeInspector.Classify(
                fileExists: true,
                document.LastWriteTimeUtc,
                File.GetLastWriteTimeUtc(fullTargetPath)) == ExternalDocumentState.ChangedOnDisk)
        {
            var choice = MessageBox.Show(this,
                $"{document.DisplayName} changed on disk after it was loaded here.\n\nOverwrite the newer copy on disk?",
                "File changed on disk", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (choice != MessageBoxResult.Yes)
            {
                return false;
            }
        }

        try
        {
            await _fileService.SaveAsync(targetPath, document.Markdown, document.HasUtf8Bom, document.LineEnding);
            document.MarkSaved(targetPath, File.GetLastWriteTimeUtc(targetPath));
            DiscardDraft(document);
            _recentFiles.Touch(fullTargetPath);
            StatusText.Text = $"Saved {document.FilePath}";
            UpdateWindowState();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, exception.Message, "Unable to save document", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void PrintPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_activeDocument is null)
        {
            return;
        }

        var preview = new PrintPreviewWindow(
            _renderer.RenderDocument(_activeDocument.Markdown, _activeDocument.DisplayName))
        { Owner = this };
        preview.Show();
    }

    private async void RichMode_Click(object sender, RoutedEventArgs e) => await SetModeAsync("rich", "Rich");

    private async void SourceMode_Click(object sender, RoutedEventArgs e) => await SetModeAsync("source", "Source");

    private async void SplitMode_Click(object sender, RoutedEventArgs e) => await SetModeAsync("split", "Split");

    private async Task SetModeAsync(string mode, string label)
    {
        if (_activeDocument is null)
        {
            return;
        }

        if (mode is "rich" or "split" && _activeDocument.RichEditorRisks.Count > 0)
        {
            var riskList = string.Join(", ", _activeDocument.RichEditorRisks);
            var result = MessageBox.Show(this,
                $"This document contains {riskList}. The formatted editor may normalize or remove that syntax. Continue?",
                "Unsupported Markdown syntax", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK)
            {
                return;
            }
        }

        _activeDocument.EditorMode = mode;
        await SendToEditorAsync(new { type = "mode", mode });
        ModeText.Text = label.ToUpperInvariant();
        UpdateWindowState();
    }

    private async void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ThemePickerWindow(_activeTheme.Id) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedThemeId is not { } themeId)
        {
            return;
        }

        _activeTheme = _themeManager.Apply(themeId);
        UpdateThemeVisualState();
        await SendThemeToEditorAsync();
        StatusText.Text = $"Theme changed to {_activeTheme.DisplayName}.";
    }

    private void UpdateThemeVisualState()
    {
        var themeLabel = _activeTheme.DisplayName.ToUpperInvariant();
        ThemeButtonText.Text = $"PALETTE / {themeLabel}";
        ThemeStatusText.Text = themeLabel;
        EditorWebView.DefaultBackgroundColor = System.Drawing.ColorTranslator.FromHtml(_activeTheme.Background);
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Open folder", Multiselect = false };
        if (dialog.ShowDialog(this) == true)
        {
            SetFolderRoot(dialog.FolderName);
        }
    }

    private void SetFolderRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        FolderRoots.Clear();
        FolderRoots.Add(FolderTreeNode.CreateRoot(fullPath));
        var folderName = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar));
        FolderNameText.Text = string.IsNullOrEmpty(folderName) ? fullPath : folderName;
        FolderNameText.ToolTip = fullPath;
    }

    private void FolderTree_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: FolderTreeNode node })
        {
            node.LoadChildren();
        }
    }

    private async void FolderTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FolderTree.SelectedItem is FolderTreeNode { IsDirectory: false, IsPlaceholder: false } node)
        {
            await OpenPathAsync(node.FullPath);
            e.Handled = true;
        }
    }

    private async void Exit_Click(object sender, RoutedEventArgs e) => await ExitApplicationAsync();

    private async Task ExitApplicationAsync()
    {
        foreach (var document in Documents.ToArray())
        {
            if (!await ConfirmCloseDocumentAsync(document))
            {
                return;
            }
        }

        _recoveryTimer.Stop();
        TryClearRecoveryStore();
        _allowClose = true;
        _trayIcon.Visible = false;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private async Task<bool> ConfirmCloseDocumentAsync(DocumentSession document)
    {
        if (!document.IsDirty)
        {
            return true;
        }

        var result = MessageBox.Show(this, $"Save changes to {document.DisplayName}?", "Unsaved changes",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result switch
        {
            MessageBoxResult.Yes => await SaveDocumentAsync(document, forceSaveAs: false),
            MessageBoxResult.No => true,
            _ => false,
        };
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        _trayIcon.ShowBalloonTip(1200, "Markdown Editor", "The editor is still running in the system tray.",
            WinForms.ToolTipIcon.Info);
    }

    private void Window_Closed(object? sender, EventArgs e) => Dispose();

    private async void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            await OpenPathsAsync(paths);
        }
    }

    private void Recent_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        var entries = _recentFiles.Load();
        if (entries.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "No recent documents", IsEnabled = false });
        }

        foreach (var path in entries)
        {
            var item = new MenuItem
            {
                Header = Path.GetFileName(path).Replace("_", "__", StringComparison.Ordinal),
                ToolTip = path,
                Tag = path,
            };
            item.Click += async (_, _) => await OpenRecentPathAsync(path);
            menu.Items.Add(item);
        }

        menu.PlacementTarget = RecentButton;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private async Task OpenRecentPathAsync(string path)
    {
        if (!File.Exists(path))
        {
            _recentFiles.Remove(path);
            MessageBox.Show(this, $"{path} no longer exists and was removed from the recent list.",
                "File not found", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await OpenPathAsync(path);
    }

    private WinForms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Open editor", null, (_, _) => Dispatcher.Invoke(RestoreWindow));
        var recentMenu = new WinForms.ToolStripMenuItem("Open recent");
        menu.Items.Add(recentMenu);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.InvokeAsync(ExitApplicationAsync));
        menu.Opening += (_, _) =>
        {
            recentMenu.DropDownItems.Clear();
            var entries = _recentFiles.Load();
            if (entries.Count == 0)
            {
                recentMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("No recent documents") { Enabled = false });
                return;
            }

            foreach (var path in entries)
            {
                var item = new WinForms.ToolStripMenuItem(Path.GetFileName(path)) { ToolTipText = path };
                item.Click += (_, _) => Dispatcher.InvokeAsync(async () =>
                {
                    RestoreWindow();
                    await OpenRecentPathAsync(path);
                });
                recentMenu.DropDownItems.Add(item);
            }
        };
        return menu;
    }

    private void RestoreWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void UpdateWindowState()
    {
        var document = _activeDocument;
        Title = document is null ? "Markdown Editor" : $"{document.Header} — Markdown Editor";
        var editorMode = document?.EditorMode ?? "rich";
        ModeText.Text = editorMode switch
        {
            "source" => "SOURCE",
            "split" => "SPLIT",
            _ => "RICH",
        };
        RichModeButton.IsChecked = editorMode == "rich";
        SourceModeButton.IsChecked = editorMode == "source";
        SplitModeButton.IsChecked = editorMode == "split";
        StatusText.Text = document switch
        {
            null => "No document open",
            { RichEditorRisks.Count: > 0, EditorMode: "source" } =>
                "Source mode preserves syntax outside the formatted editor's compatibility set.",
            { FilePath: not null } => document.FilePath,
            _ => "Untitled document",
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _recoveryTimer.Stop();
        _trayIcon.Dispose();
        EditorWebView.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
