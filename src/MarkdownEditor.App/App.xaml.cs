using System.IO;
using System.Windows;

namespace MarkdownEditor.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var initialPath = e.Args
            .Select(Path.GetFullPath)
            .FirstOrDefault(File.Exists);

        var window = new MainWindow(initialPath);
        MainWindow = window;
        window.Show();
    }
}
