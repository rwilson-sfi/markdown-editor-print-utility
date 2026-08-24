using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace MarkdownEditor.App;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _listenerTask;
    private bool _disposed;

    public SingleInstanceCoordinator()
    {
        var userScope = string.Concat(Environment.UserName.Select(character =>
            char.IsLetterOrDigit(character) ? character : '_'));
        var instanceName = $"MarkdownEditorUtility-v1-{userScope}";
        _pipeName = instanceName;
        _mutex = new Mutex(initiallyOwned: true, $"Local\\{instanceName}", out var isPrimary);
        IsPrimary = isPrimary;
    }

    public bool IsPrimary { get; }

    public void StartListening(Func<IReadOnlyList<string>, Task> onOpenFiles)
    {
        ArgumentNullException.ThrowIfNull(onOpenFiles);
        if (!IsPrimary || _listenerTask is not null)
        {
            return;
        }

        _listenerTask = Task.Run(() => ListenAsync(onOpenFiles, _shutdown.Token));
    }

    public async Task SendOpenFilesAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        await using var client = new NamedPipeClientStream(
            serverName: ".",
            pipeName: _pipeName,
            direction: PipeDirection.Out,
            options: PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await client.ConnectAsync(timeout.Token).ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(client, paths, cancellationToken: timeout.Token).ConfigureAwait(false);
        await client.FlushAsync(timeout.Token).ConfigureAwait(false);
    }

    private async Task ListenAsync(Func<IReadOnlyList<string>, Task> onOpenFiles, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                var paths = await JsonSerializer.DeserializeAsync<string[]>(server, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (paths is not null)
                {
                    await onOpenFiles(paths).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                // A client can disappear between connecting and sending. Keep the primary listener alive.
            }
            catch (JsonException)
            {
                // Ignore malformed local messages and continue serving valid open requests.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _shutdown.Cancel();
        _shutdown.Dispose();
        if (IsPrimary)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
