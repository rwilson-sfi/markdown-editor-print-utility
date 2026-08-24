using System.Text;

namespace MarkdownEditor.Core.Documents;

public sealed class DocumentFileService
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    public async Task<MarkdownFile> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var hasBom = bytes.AsSpan().StartsWith(Utf8Bom);
        var content = hasBom ? bytes.AsSpan(Utf8Bom.Length) : bytes.AsSpan();

        string text;
        try
        {
            text = StrictUtf8.GetString(content);
        }
        catch (DecoderFallbackException exception)
        {
            throw new UnsupportedTextEncodingException(fullPath, exception);
        }

        return new MarkdownFile(
            fullPath,
            text,
            hasBom,
            DetectLineEnding(text),
            File.GetLastWriteTimeUtc(fullPath));
    }

    public async Task SaveAsync(
        string path,
        string text,
        bool includeUtf8Bom,
        LineEnding lineEnding,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new IOException($"Cannot determine the directory for '{fullPath}'.");

        Directory.CreateDirectory(directory);

        var normalized = NormalizeLineEndings(text, lineEnding);
        var contentBytes = StrictUtf8.GetBytes(normalized);
        var bytes = includeUtf8Bom
            ? Utf8Bom.Concat(contentBytes).ToArray()
            : contentBytes;

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static LineEnding DetectLineEnding(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var crlfCount = 0;
        var lfCount = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n')
            {
                continue;
            }

            if (index > 0 && text[index - 1] == '\r')
            {
                crlfCount++;
            }
            else
            {
                lfCount++;
            }
        }

        return crlfCount >= lfCount && crlfCount > 0
            ? LineEnding.CrLf
            : LineEnding.Lf;
    }

    public static string NormalizeLineEndings(string text, LineEnding lineEnding)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lfOnly = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return lineEnding == LineEnding.CrLf
            ? lfOnly.Replace("\n", "\r\n", StringComparison.Ordinal)
            : lfOnly;
    }
}
