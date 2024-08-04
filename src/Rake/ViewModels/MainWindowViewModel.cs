using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flurl;
using Microsoft.Extensions.Logging;
using Rake.Generator.Attributes;
using Rake.ViewModels.Common;
using WebViewControl;

namespace Rake.ViewModels;

[Singleton]
public partial class MainWindowViewModel : BaseViewModel
{
    private readonly ILogger<MainWindowViewModel> _logger;
    [ObservableProperty] private string _greeting = "Test";

    [ObservableProperty] private Url _address = "https://www.google.com/";

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
    {
        _logger = logger;

        // Address = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
    }
    
    [RelayCommand]
    private void Initialized(WebView webView)
    {
        webView.Address = Address;
        // webView.Url = Address.ToUri();
    }
}