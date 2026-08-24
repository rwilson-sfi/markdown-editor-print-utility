using System.Text;
using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.Core.Tests;

public sealed class DocumentFileServiceTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "MarkdownEditor.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAsync_PreservesBomAndDetectsCrLf()
    {
        Directory.CreateDirectory(_temporaryDirectory);
        var path = Path.Combine(_temporaryDirectory, "bom-crlf.md");
        var content = "# Heading\r\n\r\n- Item\r\n";
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray());

        var result = await new DocumentFileService().LoadAsync(path);

        Assert.Equal(content, result.Text);
        Assert.True(result.HasUtf8Bom);
        Assert.Equal(LineEnding.CrLf, result.LineEnding);
    }

    [Fact]
    public async Task SaveAsync_WritesUtf8WithoutBomAndPreservesLf()
    {
        Directory.CreateDirectory(_temporaryDirectory);
        var path = Path.Combine(_temporaryDirectory, "lf.md");

        await new DocumentFileService().SaveAsync(
            path,
            "# Heading\r\n\r\nParagraph\r\n",
            includeUtf8Bom: false,
            LineEnding.Lf);

        var bytes = await File.ReadAllBytesAsync(path);
        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Equal("# Heading\n\nParagraph\n", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task LoadAsync_RejectsInvalidUtf8()
    {
        Directory.CreateDirectory(_temporaryDirectory);
        var path = Path.Combine(_temporaryDirectory, "legacy.md");
        await File.WriteAllBytesAsync(path, [0x48, 0x80, 0x49]);

        await Assert.ThrowsAsync<UnsupportedTextEncodingException>(
            () => new DocumentFileService().LoadAsync(path));
    }

    [Theory]
    [InlineData("a\nb\n", LineEnding.Lf)]
    [InlineData("a\r\nb\r\n", LineEnding.CrLf)]
    [InlineData("single line", LineEnding.Lf)]
    public void DetectLineEnding_ReturnsExpectedStyle(string text, LineEnding expected)
    {
        Assert.Equal(expected, DocumentFileService.DetectLineEnding(text));
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
