using System.IO;
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
            catch (Exception exception) when (exception is IOException or TimeoutException or OperationCanceledException)
            {
                System.Windows.MessageBox.Show(
                    $"The running Markdown Editor could not receive the open request.\n\n{exception.Message}",
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
