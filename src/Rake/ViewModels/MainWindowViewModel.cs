using CommunityToolkit.Mvvm.ComponentModel;
using Flurl;
using Rake.Generator.Attributes;
using Rake.ViewModels.Common;

namespace Rake.ViewModels;

[Singleton]
public partial class MainWindowViewModel : BaseViewModel
{
    [ObservableProperty]
    private Url _address = "https://www.google.com/";

    [ObservableProperty]
    private string _greeting = "Test";

    public MainWindowViewModel(MainViewModel mainViewModel)
    {
        LibVLCSharp.Shared.Core.Initialize("");
        CurrentContent = mainViewModel;

        Address =
            "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
    }

    public object CurrentContent { get; }
}
