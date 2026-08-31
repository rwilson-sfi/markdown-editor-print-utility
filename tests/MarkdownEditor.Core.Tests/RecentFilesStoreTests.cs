using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.Core.Tests;

public sealed class RecentFilesStoreTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "MarkdownEditor.Tests",
        Guid.NewGuid().ToString("N"));

    private RecentFilesStore CreateStore(int capacity = RecentFilesStore.DefaultCapacity) =>
        new(Path.Combine(_temporaryDirectory, "recent-files.json"), capacity);

    [Fact]
    public void Load_ReturnsEmptyListWhenNothingWasStored()
    {
        Assert.Empty(CreateStore().Load());
    }

    [Fact]
    public void Touch_PutsMostRecentPathFirstAndPersists()
    {
        var store = CreateStore();
        store.Touch(@"C:\notes\first.md");
        store.Touch(@"C:\notes\second.md");

        Assert.Equal(
            [@"C:\notes\second.md", @"C:\notes\first.md"],
            CreateStore().Load());
    }

    [Fact]
    public void Touch_DeduplicatesPathsCaseInsensitively()
    {
        var store = CreateStore();
        store.Touch(@"C:\notes\plan.md");
        store.Touch(@"C:\NOTES\PLAN.MD");

        var entries = Assert.Single(store.Load());
        Assert.Equal(@"C:\NOTES\PLAN.MD", entries);
    }

    [Fact]
    public void Touch_EnforcesTheConfiguredCapacity()
    {
        var store = CreateStore(capacity: 3);
        for (var index = 1; index <= 5; index++)
        {
            store.Touch($@"C:\notes\file-{index}.md");
        }

        Assert.Equal(
            [@"C:\notes\file-5.md", @"C:\notes\file-4.md", @"C:\notes\file-3.md"],
            store.Load());
    }

    [Fact]
    public void Remove_DropsTheRequestedPath()
    {
        var store = CreateStore();
        store.Touch(@"C:\notes\keep.md");
        store.Touch(@"C:\notes\remove.md");

        var remaining = store.Remove(@"C:\NOTES\remove.md");

        Assert.Equal([@"C:\notes\keep.md"], remaining);
        Assert.Equal([@"C:\notes\keep.md"], store.Load());
    }

    [Fact]
    public void Load_ReturnsEmptyListForCorruptStorage()
    {
        Directory.CreateDirectory(_temporaryDirectory);
        var storagePath = Path.Combine(_temporaryDirectory, "recent-files.json");
        File.WriteAllText(storagePath, "{not json");

        Assert.Empty(new RecentFilesStore(storagePath).Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
