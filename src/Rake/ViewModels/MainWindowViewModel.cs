using AvaloniaWebView;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flurl;

namespace Rake.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Test";

    [ObservableProperty]
    private Url _address = "https://www.google.com/";

    [RelayCommand]
    private void Initialized(WebView webView)
    {
        webView.Url = Address.ToUri();
    }
}
