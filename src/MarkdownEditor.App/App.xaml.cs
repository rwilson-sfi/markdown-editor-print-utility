using System.Windows;
using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.App;

public partial class App : System.Windows.Application
{
    private SingleInstanceCoordinator? _instanceCoordinator;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var initialPaths = MarkdownPath.ParseExistingFiles(e.Args);

        _instanceCoordinator = new SingleInstanceCoordinator();
        if (!_instanceCoordinator.IsPrimary)
        {
            try
            {
                await _instanceCoordinator.SendOpenFilesAsync(initialPaths);
            }
            catch (Exception exception)
            {
                // The forwarding process has no window of its own; any hand-off failure must end in
                // this dialog rather than an unhandled crash from async void OnStartup.
                var detail = exception switch
                {
                    UnauthorizedAccessException =>
                        "The running Markdown Editor instance refused the connection. It may have been " +
                        "started with different permissions (for example, as administrator). Close that " +
                        "instance and try again.",
                    TimeoutException or OperationCanceledException =>
                        "The running Markdown Editor instance did not respond within five seconds.",
                    _ => exception.Message,
                };
                System.Windows.MessageBox.Show(
                    $"The running Markdown Editor could not receive the open request.\n\n{detail}",
                    "Unable to open document",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            _instanceCoordinator.Dispose();
            _instanceCoordinator = null;
            Shutdown();
            return;
        }

        var window = new MainWindow(initialPaths);
        MainWindow = window;
        _instanceCoordinator.StartListening(paths =>
            Dispatcher.InvokeAsync(() => window.OpenPathsFromExternalAsync(paths)).Task.Unwrap());
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceCoordinator?.Dispose();
        base.OnExit(e);
    }
}
