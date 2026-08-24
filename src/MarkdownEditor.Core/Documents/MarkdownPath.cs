namespace MarkdownEditor.Core.Documents;

public static class MarkdownPath
{
    public static bool IsSupported(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
               || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> ParseExistingFiles(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var argument in arguments.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (argument.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var fullPath = Path.GetFullPath(argument);
                if (IsSupported(fullPath) && File.Exists(fullPath))
                {
                    paths.Add(fullPath);
                }
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Ignore malformed shell arguments and preserve the running primary instance.
            }
        }

        return paths.ToArray();
    }
}
