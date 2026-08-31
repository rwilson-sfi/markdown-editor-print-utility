using System.Text.Json;

namespace MarkdownEditor.Core.Documents;

public sealed class RecentFilesStore
{
    public const int DefaultCapacity = 10;

    private readonly string _storagePath;
    private readonly int _capacity;

    public RecentFilesStore(string storagePath, int capacity = DefaultCapacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _storagePath = Path.GetFullPath(storagePath);
        _capacity = capacity;
    }

    public IReadOnlyList<string> Load()
    {
        try
        {
            if (!File.Exists(_storagePath))
            {
                return [];
            }

            var entries = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_storagePath)) ?? [];
            return Normalize(entries);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    public IReadOnlyList<string> Touch(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var entries = new List<string> { fullPath };
        entries.AddRange(Load().Where(existing =>
            !string.Equals(existing, fullPath, StringComparison.OrdinalIgnoreCase)));
        var normalized = Normalize(entries);
        Save(normalized);
        return normalized;
    }

    public IReadOnlyList<string> Remove(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var normalized = Normalize(Load().Where(existing =>
            !string.Equals(existing, fullPath, StringComparison.OrdinalIgnoreCase)));
        Save(normalized);
        return normalized;
    }

    private IReadOnlyList<string> Normalize(IEnumerable<string> entries)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(entry);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (seen.Add(fullPath))
            {
                normalized.Add(fullPath);
                if (normalized.Count == _capacity)
                {
                    break;
                }
            }
        }

        return normalized;
    }

    private void Save(IReadOnlyList<string> entries)
    {
        try
        {
            var directory = Path.GetDirectoryName(_storagePath)
                ?? throw new IOException($"Cannot determine the directory for '{_storagePath}'.");
            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory, $".recent.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(entries));
                File.Move(temporaryPath, _storagePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Recent-file persistence is a convenience; failing to write must not block editing.
        }
    }
}
