using System.Text.Json;

namespace MarkdownEditor.Core.Recovery;

public sealed class RecoveryDraftStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
    };

    private readonly string _directory;

    public RecoveryDraftStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = Path.GetFullPath(directory);
    }

    public async Task SaveDraftAsync(RecoveryDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.Id);

        Directory.CreateDirectory(_directory);
        var payload = JsonSerializer.Serialize(draft, SerializerOptions);
        var targetPath = DraftPath(draft.Id);
        var temporaryPath = Path.Combine(_directory, $".{draft.Id}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, payload, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public IReadOnlyList<RecoveryDraft> LoadDrafts()
    {
        if (!Directory.Exists(_directory))
        {
            return [];
        }

        var drafts = new List<RecoveryDraft>();
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                var draft = JsonSerializer.Deserialize<RecoveryDraft>(File.ReadAllText(path));
                if (draft is not null && !string.IsNullOrWhiteSpace(draft.Id) && draft.Markdown is not null)
                {
                    drafts.Add(draft);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                // A corrupt or unreadable draft must not block recovery of the remaining drafts.
            }
        }

        return drafts.OrderBy(draft => draft.SavedAtUtc).ToArray();
    }

    public void DeleteDraft(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var path = DraftPath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void Clear()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            File.Delete(path);
        }
    }

    private string DraftPath(string id)
    {
        var fileName = $"{id}.json";
        var path = Path.GetFullPath(Path.Combine(_directory, fileName));
        if (!string.Equals(Path.GetDirectoryName(path), _directory, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetFileName(path), fileName, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Draft id '{id}' is not a valid file name.", nameof(id));
        }

        return path;
    }
}
