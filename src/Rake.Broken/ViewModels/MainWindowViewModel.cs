using CommunityToolkit.Mvvm.ComponentModel;
using Rake.Generator.Attributes;
using Rake.ViewModels.Common;

namespace Rake.ViewModels;

[Singleton]
public partial class MainWindowViewModel : BaseViewModel
{
    [ObservableProperty]
    private object _current;

    [ObservableProperty]
    private bool _dialogCloseOnClickAway;

    [ObservableProperty]
    private string _greeting = "Hello";

    [ObservableProperty]
    private string _downloadProgress = "Progress: 0%";

    public MainWindowViewModel(MainViewModel mainViewModel)
    {
        Current = mainViewModel;
    }
}
