using Rake.ViewModels.Common;

namespace Rake.ViewModels;

public class MainViewModel : BaseViewModel
{
    protected override void OnLoaded()
    {
        // using var media = new Media(
        //     LibVlc,
        //     "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
        //     FromType.FromLocation
        // );
        // MediaPlayer.Play(media);
    }
}
