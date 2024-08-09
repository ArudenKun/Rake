using Rake.Core.Helpers;
using static Unosquare.FFME.Library;

namespace Rake;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public sealed partial class App
{
    public App()
    {
        InitializeComponent();

        FFmpegDirectory = EnvironmentHelper.AppDirectory.JoinPath("bin", "ffmpeg");
    }
}
