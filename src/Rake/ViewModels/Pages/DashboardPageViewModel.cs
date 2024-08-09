using CommunityToolkit.Mvvm.Input;
using Rake.ViewModels.Abstractions;
using Rake.Views.Pages;
using Unosquare.FFME;

namespace Rake.ViewModels.Pages;

public sealed partial class DashboardPageViewModel : BasePageViewModel
{
    public override int Index { get; } = 1;
    public override Type ViewType { get; } = typeof(DashboardPage);

    public DashboardPageViewModel() { }

    [RelayCommand]
    private async Task Initialize(MediaElement mediaElement)
    {
        await mediaElement.Open(
            new Uri(
                "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
            )
        );
    }
}
