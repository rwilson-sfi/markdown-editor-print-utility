using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using MarkdownEditor.Core.Documents;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using DataFormats = System.Windows.DataFormats;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WinForms = System.Windows.Forms;

namespace MarkdownEditor.App;

public partial class MainWindow : Window, IDisposable
{
    private readonly DocumentFileService _fileService = new();
    private readonly MarkdownRenderer _renderer = new();
    private readonly string? _initialPath;
    private readonly WinForms.NotifyIcon _trayIcon;

    private string _markdown = "# Untitled\n\nStart writing…\n";
    private string _lastSavedMarkdown = string.Empty;
    private IReadOnlyList<string> _richEditorRisks = [];
    private string? _filePath;
    private bool _hasUtf8Bom;
    private LineEnding _lineEnding = LineEnding.CrLf;
    private string _editorMode = "rich";
    private bool _editorReady;
    private bool _allowClose;
    private bool _disposed;

    public MainWindow(string? initialPath = null)
    {
        _initialPath = initialPath;
        InitializeComponent();

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Markdown Editor",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu(),
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(RestoreWindow);

        UpdateTitle();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeEditorAsync();

        if (_initialPath is not null)
        {
            await OpenPathAsync(_initialPath);
        }
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
            MessageBox.Show(
                this,
                "Microsoft Edge WebView2 Runtime is required to use the formatted editor.",
                "WebView2 Runtime not found",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
            "appassets.local",
            assetDirectory,
            CoreWebView2HostResourceAccessKind.DenyCors);
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
                await SendToEditorAsync(new { type = "load", markdown = _markdown });
                await SendToEditorAsync(new { type = "mode", mode = _editorMode });
                StatusText.Text = _richEditorRisks.Count > 0
                    ? "Opened in Source mode to preserve unsupported Markdown syntax."
                    : _filePath is null ? "Untitled document" : _filePath;
                break;

            case "changed" when root.TryGetProperty("markdown", out var markdownProperty):
                _markdown = markdownProperty.GetString() ?? string.Empty;
                _richEditorRisks = MarkdownCompatibilityInspector.FindRichEditorRisks(_markdown);
                UpdateTitle();
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

    private async void New_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmReplaceAsync())
        {
            return;
        }

        _filePath = null;
        _markdown = "# Untitled\n\nStart writing…\n";
        _lastSavedMarkdown = string.Empty;
        _richEditorRisks = [];
        _hasUtf8Bom = false;
        _lineEnding = LineEnding.CrLf;
        _editorMode = "rich";
        await SendToEditorAsync(new { type = "load", markdown = _markdown });
        await SendToEditorAsync(new { type = "mode", mode = _editorMode });
        ModeText.Text = "Rich";
        StatusText.Text = "Untitled document";
        UpdateTitle();
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmReplaceAsync())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Open Markdown document",
            Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) == true)
        {
            await OpenPathAsync(dialog.FileName);
        }
    }

    private async Task OpenPathAsync(string path)
    {
        try
        {
            var document = await _fileService.LoadAsync(path);
            _filePath = document.Path;
            _markdown = document.Text;
            _lastSavedMarkdown = document.Text;
            _richEditorRisks = MarkdownCompatibilityInspector.FindRichEditorRisks(document.Text);
            _hasUtf8Bom = document.HasUtf8Bom;
            _lineEnding = document.LineEnding;
            _editorMode = _richEditorRisks.Count > 0 ? "source" : "rich";
            await SendToEditorAsync(new { type = "load", markdown = _markdown });
            await SendToEditorAsync(new { type = "mode", mode = _editorMode });
            if (_richEditorRisks.Count > 0)
            {
                ModeText.Text = "Source";
                StatusText.Text = "Opened in Source mode to preserve unsupported Markdown syntax.";
            }
            else
            {
                StatusText.Text = document.Path;
            }

            UpdateTitle();
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

    private async void Save_Click(object sender, RoutedEventArgs e) => await SaveAsync(forceSaveAs: false);

    private async void SaveAs_Click(object sender, RoutedEventArgs e) => await SaveAsync(forceSaveAs: true);

    private async Task<bool> SaveAsync(bool forceSaveAs)
    {
        var targetPath = _filePath;
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
        }

        try
        {
            await _fileService.SaveAsync(targetPath, _markdown, _hasUtf8Bom, _lineEnding);
            _filePath = Path.GetFullPath(targetPath);
            _lastSavedMarkdown = _markdown;
            StatusText.Text = $"Saved {_filePath}";
            UpdateTitle();
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
        var preview = new PrintPreviewWindow(
            _renderer.RenderDocument(_markdown, _filePath is null ? "Untitled" : Path.GetFileName(_filePath)))
        {
            Owner = this,
        };
        preview.Show();
    }

    private async void RichMode_Click(object sender, RoutedEventArgs e) => await SetModeAsync("rich", "Rich");

    private async void SourceMode_Click(object sender, RoutedEventArgs e) => await SetModeAsync("source", "Source");

    private async void SplitMode_Click(object sender, RoutedEventArgs e) => await SetModeAsync("split", "Split");

    private async Task SetModeAsync(string mode, string label)
    {
        if (mode is "rich" or "split" && _richEditorRisks.Count > 0)
        {
            var riskList = string.Join(", ", _richEditorRisks);
            var result = MessageBox.Show(
                this,
                $"This document contains {riskList}. The formatted editor may normalize or remove that syntax. Continue?",
                "Unsupported Markdown syntax",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
            {
                return;
            }
        }

        _editorMode = mode;
        await SendToEditorAsync(new { type = "mode", mode });
        ModeText.Text = label;
    }

    private async void Exit_Click(object sender, RoutedEventArgs e) => await ExitApplicationAsync();

    private async Task ExitApplicationAsync()
    {
        if (!await ConfirmReplaceAsync())
        {
            return;
        }

        _allowClose = true;
        _trayIcon.Visible = false;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private async Task<bool> ConfirmReplaceAsync()
    {
        if (!IsDirty)
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
            "Save changes to the current Markdown document?",
            "Unsaved changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => await SaveAsync(forceSaveAs: false),
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
        _trayIcon.ShowBalloonTip(
            1200,
            "Markdown Editor",
            "The editor is still running in the system tray.",
            WinForms.ToolTipIcon.Info);
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }

        var markdownPath = paths.FirstOrDefault(path =>
            string.Equals(Path.GetExtension(path), ".md", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(path), ".markdown", StringComparison.OrdinalIgnoreCase));

        if (markdownPath is not null)
        {
            _ = OpenDroppedPathAsync(markdownPath);
        }
    }

    private async Task OpenDroppedPathAsync(string path)
    {
        if (await ConfirmReplaceAsync())
        {
            await OpenPathAsync(path);
        }
    }

    private WinForms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Open editor", null, (_, _) => Dispatcher.Invoke(RestoreWindow));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.InvokeAsync(ExitApplicationAsync));
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
    }

    private bool IsDirty => !string.Equals(_markdown, _lastSavedMarkdown, StringComparison.Ordinal);

    private void UpdateTitle()
    {
        var name = _filePath is null ? "Untitled" : Path.GetFileName(_filePath);
        Title = $"{(IsDirty ? "*" : string.Empty)}{name} — Markdown Editor";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _trayIcon.Dispose();
        EditorWebView.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
