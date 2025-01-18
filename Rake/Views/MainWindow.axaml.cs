using Rake.ViewModels;
using WebViewCore.Enums;
using WebViewCore.Events;

namespace Rake.Views;

public sealed partial class MainWindow : AbstractSukiWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        // WebView.WebViewNewWindowRequested += WebViewOnWebViewNewWindowRequested;
    }

    private void WebViewOnWebViewNewWindowRequested(object? sender, WebViewNewWindowEventArgs e)
    {
        e.UrlLoadingStrategy = UrlRequestStrategy.OpenInWebView;
    }
}
