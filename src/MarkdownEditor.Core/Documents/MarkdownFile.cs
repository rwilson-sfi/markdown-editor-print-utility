namespace MarkdownEditor.Core.Documents;

public sealed record MarkdownFile(
    string Path,
    string Text,
    bool HasUtf8Bom,
    LineEnding LineEnding,
    DateTimeOffset LastWriteTimeUtc);
