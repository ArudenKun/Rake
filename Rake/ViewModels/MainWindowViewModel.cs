using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Rake.Core.Tools.FFmpeg;
using Rake.Core.Tools.HandBrake;
using Rake.Core.Tools.YtDlp;

namespace Rake.ViewModels;

public partial class MainWindowViewModel : ViewModel
{
    public MainWindowViewModel(
        IFFmpegToolsService fFmpegToolService,
        IYtDlpToolsService ytDlpToolService,
        IHandBrakeToolsService handBrakeToolService
    )
    {
        FFmpegToolService = fFmpegToolService;
        YtDlpToolService = ytDlpToolService;
        HandBrakeToolService = handBrakeToolService;
    }

    protected IFFmpegToolsService FFmpegToolService { get; }
    protected IYtDlpToolsService YtDlpToolService { get; }
    protected IHandBrakeToolsService HandBrakeToolService { get; }

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [RelayCommand]
    private async Task ShowExceptionAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("HandBrake Path: {Path}", RakePathConsts.HandBrake);
        Logger.LogInformation("FFmpeg Path: {Path}", RakePathConsts.FFmpeg);
        Logger.LogInformation("yt-dlp Path: {Path}", RakePathConsts.YtDlp);
        var isHandBraleAvailable = HandBrakeToolService.IsAvailable(RakePathConsts.HandBrake);
        var isFFmpegAvailable = FFmpegToolService.IsAvailable(RakePathConsts.FFmpeg);
        var isYtDlpAvailable = HandBrakeToolService.IsAvailable(RakePathConsts.YtDlp);

        var handBrakeVersionTask = HandBrakeToolService.GetVersionAsync(
            RakePathConsts.HandBrake,
            cancellationToken
        );
        var ffmpegVersionTask = FFmpegToolService.GetVersionAsync(
            RakePathConsts.FFmpeg,
            cancellationToken
        );
        var ytDlpVersionTask = YtDlpToolService.GetVersionAsync(
            RakePathConsts.YtDlp,
            cancellationToken
        );

        await Task.WhenAll(handBrakeVersionTask, ffmpegVersionTask, ytDlpVersionTask);

        var sb = new StringBuilder();
        sb.AppendLine($"HandBrake Version: {handBrakeVersionTask.Result}");
        sb.AppendLine($"FFmpeg Version: {ffmpegVersionTask.Result}");
        sb.AppendLine($"YtDlp Version: {ytDlpVersionTask.Result}");
        sb.AppendLine("---------");
        sb.AppendLine($"HandBrake Version: {new Version(handBrakeVersionTask.Result)}");
        sb.AppendLine($"FFmpeg Version: {new Version(ffmpegVersionTask.Result)}");
        sb.AppendLine($"YtDlp Version: {new Version(ytDlpVersionTask.Result)}");

        Greeting = sb.ToString();
    }
}
