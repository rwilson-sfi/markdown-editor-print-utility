using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.Core.Tests;

public sealed class MarkdownPathTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"MarkdownEditorPathTests-{Guid.NewGuid():N}");

    public MarkdownPathTests() => Directory.CreateDirectory(_temporaryDirectory);

    [Theory]
    [InlineData("notes.md")]
    [InlineData("notes.MARKDOWN")]
    public void IsSupported_AcceptsMarkdownExtensions(string path) =>
        Assert.True(MarkdownPath.IsSupported(path));

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("notes")]
    public void IsSupported_RejectsOtherExtensions(string path) =>
        Assert.False(MarkdownPath.IsSupported(path));

    [Fact]
    public void ParseExistingFiles_IgnoresFlagsMissingFilesAndDuplicates()
    {
        var markdownPath = Path.Combine(_temporaryDirectory, "notes.md");
        var textPath = Path.Combine(_temporaryDirectory, "notes.txt");
        File.WriteAllText(markdownPath, "# Notes");
        File.WriteAllText(textPath, "not Markdown");

        var result = MarkdownPath.ParseExistingFiles([
            "--edit",
            markdownPath,
            markdownPath.ToUpperInvariant(),
            textPath,
            Path.Combine(_temporaryDirectory, "missing.md"),
        ]);

        Assert.Equal([Path.GetFullPath(markdownPath)], result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
