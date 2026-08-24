namespace MarkdownEditor.Core.Documents;

public sealed class UnsupportedTextEncodingException(string path, Exception? innerException = null)
    : IOException($"'{path}' is not valid UTF-8. Conversion requires explicit confirmation.", innerException)
{
    public string FilePath { get; } = path;
}
