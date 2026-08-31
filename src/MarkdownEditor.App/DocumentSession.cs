using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.App;

public sealed class DocumentSession : INotifyPropertyChanged
{
    private string _markdown;
    private string _lastSavedMarkdown;
    private string? _filePath;
    private bool _isRecoveredDraft;
    private bool _isMissingOnDisk;

    private DocumentSession(
        string? filePath,
        string markdown,
        string lastSavedMarkdown,
        bool hasUtf8Bom,
        LineEnding lineEnding,
        DateTimeOffset lastWriteTimeUtc)
    {
        Id = Guid.NewGuid().ToString("N");
        _filePath = filePath;
        _markdown = markdown;
        _lastSavedMarkdown = lastSavedMarkdown;
        HasUtf8Bom = hasUtf8Bom;
        LineEnding = lineEnding;
        LastWriteTimeUtc = lastWriteTimeUtc;
        RichEditorRisks = MarkdownCompatibilityInspector.FindRichEditorRisks(markdown);
        EditorMode = RichEditorRisks.Count > 0 ? "source" : "rich";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string? FilePath => _filePath;

    public string Markdown => _markdown;

    public bool HasUtf8Bom { get; private set; }

    public LineEnding LineEnding { get; private set; }

    public DateTimeOffset LastWriteTimeUtc { get; private set; }

    public string EditorMode { get; set; }

    public IReadOnlyList<string> RichEditorRisks { get; private set; }

    // Saving normalizes every line ending to the document's recorded style, so a
    // line-ending-only difference from the editor surface is not a real modification.
    public bool IsDirty => _isRecoveredDraft
        || _isMissingOnDisk
        || !string.Equals(
            DocumentFileService.NormalizeLineEndings(_markdown, LineEnding.Lf),
            DocumentFileService.NormalizeLineEndings(_lastSavedMarkdown, LineEnding.Lf),
            StringComparison.Ordinal);

    public string DisplayName => _filePath is null ? "Untitled" : Path.GetFileName(_filePath);

    public string Header => $"{(IsDirty ? "● " : string.Empty)}{DisplayName}";

    // The placeholder template is not user-authored content, so an untouched
    // untitled document is clean: no close prompt and no recovery draft.
    public static DocumentSession CreateUntitled() => new(
        filePath: null,
        markdown: "# Untitled\n\nStart writing…\n",
        lastSavedMarkdown: "# Untitled\n\nStart writing…\n",
        hasUtf8Bom: false,
        lineEnding: LineEnding.CrLf,
        lastWriteTimeUtc: DateTimeOffset.MinValue);

    public static DocumentSession FromFile(MarkdownFile file) => new(
        file.Path,
        file.Text,
        file.Text,
        file.HasUtf8Bom,
        file.LineEnding,
        file.LastWriteTimeUtc);

    public static DocumentSession FromRecoveredDraft(
        string? filePath,
        string markdown,
        bool hasUtf8Bom,
        LineEnding lineEnding)
    {
        var session = new DocumentSession(
            filePath is null ? null : Path.GetFullPath(filePath),
            markdown,
            markdown,
            hasUtf8Bom,
            lineEnding,
            DateTimeOffset.MinValue)
        {
            _isRecoveredDraft = true,
        };
        return session;
    }

    public void ReloadFromFile(MarkdownFile file)
    {
        _filePath = file.Path;
        _markdown = file.Text;
        _lastSavedMarkdown = file.Text;
        HasUtf8Bom = file.HasUtf8Bom;
        LineEnding = file.LineEnding;
        LastWriteTimeUtc = file.LastWriteTimeUtc;
        _isRecoveredDraft = false;
        _isMissingOnDisk = false;
        RichEditorRisks = MarkdownCompatibilityInspector.FindRichEditorRisks(file.Text);
        if (RichEditorRisks.Count > 0)
        {
            EditorMode = "source";
        }

        OnPropertyChanged(nameof(Markdown));
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Header));
    }

    public void MarkMissingOnDisk()
    {
        if (_isMissingOnDisk)
        {
            return;
        }

        _isMissingOnDisk = true;
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Header));
    }

    public void SetMarkdown(string markdown)
    {
        if (string.Equals(_markdown, markdown, StringComparison.Ordinal))
        {
            return;
        }

        _markdown = markdown;
        RichEditorRisks = MarkdownCompatibilityInspector.FindRichEditorRisks(markdown);
        OnPropertyChanged(nameof(Markdown));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Header));
    }

    public void MarkSaved(string path, DateTimeOffset lastWriteTimeUtc)
    {
        _filePath = Path.GetFullPath(path);
        _lastSavedMarkdown = _markdown;
        LastWriteTimeUtc = lastWriteTimeUtc;
        _isRecoveredDraft = false;
        _isMissingOnDisk = false;
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Header));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
