using MarkdownEditor.Core.Documents;
using MarkdownEditor.Core.Recovery;

namespace MarkdownEditor.Core.Tests;

public sealed class RecoveryDraftStoreTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "MarkdownEditor.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveDraftAsync_RoundTripsDraftContentAndMetadata()
    {
        var store = new RecoveryDraftStore(_temporaryDirectory);
        var draft = new RecoveryDraft(
            "doc1",
            @"C:\notes\plan.md",
            "# Plan\r\n\r\nDraft body\r\n",
            HasUtf8Bom: true,
            LineEnding.CrLf,
            new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));

        await store.SaveDraftAsync(draft);
        var loaded = store.LoadDrafts();

        Assert.Equal([draft], loaded);
    }

    [Fact]
    public async Task SaveDraftAsync_OverwritesExistingDraftForSameId()
    {
        var store = new RecoveryDraftStore(_temporaryDirectory);
        var first = new RecoveryDraft("doc1", null, "first", false, LineEnding.Lf, DateTimeOffset.UnixEpoch);

        await store.SaveDraftAsync(first);
        await store.SaveDraftAsync(first with { Markdown = "second" });

        var loaded = Assert.Single(store.LoadDrafts());
        Assert.Equal("second", loaded.Markdown);
    }

    [Fact]
    public async Task LoadDrafts_SkipsCorruptFilesAndOrdersBySaveTime()
    {
        var store = new RecoveryDraftStore(_temporaryDirectory);
        var older = new RecoveryDraft("older", null, "a", false, LineEnding.Lf, DateTimeOffset.UnixEpoch);
        var newer = older with { Id = "newer", SavedAtUtc = DateTimeOffset.UnixEpoch.AddMinutes(5) };
        await store.SaveDraftAsync(newer);
        await store.SaveDraftAsync(older);
        await File.WriteAllTextAsync(Path.Combine(_temporaryDirectory, "corrupt.json"), "{not json");

        var loaded = store.LoadDrafts();

        Assert.Equal(["older", "newer"], loaded.Select(draft => draft.Id));
    }

    [Fact]
    public async Task DeleteDraft_RemovesOnlyTheRequestedDraft()
    {
        var store = new RecoveryDraftStore(_temporaryDirectory);
        var draft = new RecoveryDraft("keep", null, "a", false, LineEnding.Lf, DateTimeOffset.UnixEpoch);
        await store.SaveDraftAsync(draft);
        await store.SaveDraftAsync(draft with { Id = "remove" });

        store.DeleteDraft("remove");
        store.DeleteDraft("remove");

        var loaded = Assert.Single(store.LoadDrafts());
        Assert.Equal("keep", loaded.Id);
    }

    [Fact]
    public async Task Clear_RemovesAllDrafts()
    {
        var store = new RecoveryDraftStore(_temporaryDirectory);
        await store.SaveDraftAsync(new RecoveryDraft("doc1", null, "a", false, LineEnding.Lf, DateTimeOffset.UnixEpoch));

        store.Clear();

        Assert.Empty(store.LoadDrafts());
    }

    [Fact]
    public async Task SaveDraftAsync_RejectsIdsThatEscapeTheStoreDirectory()
    {
        var store = new RecoveryDraftStore(_temporaryDirectory);
        var draft = new RecoveryDraft(@"..\escape", null, "a", false, LineEnding.Lf, DateTimeOffset.UnixEpoch);

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveDraftAsync(draft));
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
