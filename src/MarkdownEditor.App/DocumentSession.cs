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

    public bool IsDirty => !string.Equals(_markdown, _lastSavedMarkdown, StringComparison.Ordinal);

    public string DisplayName => _filePath is null ? "Untitled" : Path.GetFileName(_filePath);

    public string Header => $"{(IsDirty ? "● " : string.Empty)}{DisplayName}";

    public static DocumentSession CreateUntitled() => new(
        filePath: null,
        markdown: "# Untitled\n\nStart writing…\n",
        lastSavedMarkdown: string.Empty,
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
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Header));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
