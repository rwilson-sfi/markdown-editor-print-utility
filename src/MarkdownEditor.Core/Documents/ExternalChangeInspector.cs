namespace MarkdownEditor.Core.Documents;

public enum ExternalDocumentState
{
    Unchanged,
    ChangedOnDisk,
    MissingOnDisk,
}

public static class ExternalChangeInspector
{
    public static ExternalDocumentState Classify(
        bool fileExists,
        DateTimeOffset recordedWriteTimeUtc,
        DateTimeOffset? currentWriteTimeUtc)
    {
        if (!fileExists)
        {
            return ExternalDocumentState.MissingOnDisk;
        }

        if (currentWriteTimeUtc is null || recordedWriteTimeUtc == DateTimeOffset.MinValue)
        {
            return ExternalDocumentState.Unchanged;
        }

        return currentWriteTimeUtc == recordedWriteTimeUtc
            ? ExternalDocumentState.Unchanged
            : ExternalDocumentState.ChangedOnDisk;
    }
}
