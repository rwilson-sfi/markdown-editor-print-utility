using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.Core.Recovery;

public sealed record RecoveryDraft(
    string Id,
    string? OriginalPath,
    string Markdown,
    bool HasUtf8Bom,
    LineEnding LineEnding,
    DateTimeOffset SavedAtUtc);
