using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace MarkdownEditor.App;

public partial class PrintPreviewWindow : Window
{
    private readonly string _html;

    public PrintPreviewWindow(string html)
    {
        _html = html;
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await PreviewWebView.EnsureCoreWebView2Async();
        PreviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        PreviewWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        PreviewWebView.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
        PreviewWebView.NavigateToString(_html);
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        PreviewWebView.CoreWebView2?.ShowPrintUI(CoreWebView2PrintDialogKind.System);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
