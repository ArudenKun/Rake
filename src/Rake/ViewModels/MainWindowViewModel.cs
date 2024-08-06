using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flurl;
using Microsoft.Extensions.Logging;
using Rake.Controls.Console;
using Rake.Generator.Attributes;
using Rake.ViewModels.Common;

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
    private async Task Initialized()
    {
        // while (true)
        // {
        //     await consoleView.ViewWriter.WriteAsync($"Test: {Random.Shared.Next()}");
        //     await Task.Delay(TimeSpan.FromSeconds(3));
        // }
        // webView.Address = Address;
        // webView.Url = Address.ToUri();
    }
}