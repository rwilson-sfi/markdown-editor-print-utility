using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.Core.Tests;

public sealed class ExternalChangeInspectorTests
{
    private static readonly DateTimeOffset Recorded = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Classify_ReturnsMissingWhenFileDoesNotExist()
    {
        Assert.Equal(
            ExternalDocumentState.MissingOnDisk,
            ExternalChangeInspector.Classify(fileExists: false, Recorded, currentWriteTimeUtc: null));
    }

    [Fact]
    public void Classify_ReturnsUnchangedForMatchingTimestamp()
    {
        Assert.Equal(
            ExternalDocumentState.Unchanged,
            ExternalChangeInspector.Classify(fileExists: true, Recorded, Recorded));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void Classify_ReturnsChangedForDifferentTimestamp(int secondsOffset)
    {
        Assert.Equal(
            ExternalDocumentState.ChangedOnDisk,
            ExternalChangeInspector.Classify(fileExists: true, Recorded, Recorded.AddSeconds(secondsOffset)));
    }

    [Fact]
    public void Classify_TreatsNeverSavedDocumentsAsUnchanged()
    {
        Assert.Equal(
            ExternalDocumentState.Unchanged,
            ExternalChangeInspector.Classify(fileExists: true, DateTimeOffset.MinValue, Recorded));
    }

    [Fact]
    public void Classify_TreatsUnknownCurrentTimestampAsUnchanged()
    {
        Assert.Equal(
            ExternalDocumentState.Unchanged,
            ExternalChangeInspector.Classify(fileExists: true, Recorded, currentWriteTimeUtc: null));
    }
}
