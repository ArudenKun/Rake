using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using Humanizer;
using Microsoft.Extensions.Logging;
using Rake.Core;
using Rake.Core.Extensions;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Rake.ViewModels;

public partial class MainWindowViewModel : ViewModel
{
    public MainWindowViewModel(IToolsService toolsService, IHttpClientFactory httpClientFactory)
    {
        ToolsService = toolsService;
        YoutubeClient = new YoutubeClient(httpClientFactory.CreateClient());
    }

    protected IToolsService ToolsService { get; }
    protected YoutubeClient YoutubeClient { get; }

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [RelayCommand]
    private async Task ShowExceptionAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("QuickJs Path: {Path}", RakePathConsts.QuickJs);
        Logger.LogInformation("FFmpeg Path: {Path}", RakePathConsts.FFmpeg);
        Logger.LogInformation("yt-dlp Path: {Path}", RakePathConsts.YtDlp);
        Logger.LogInformation("Aria2 Path: {Path}", RakePathConsts.Aria2);

        foreach (var tool in Enum.GetValues<Tool>())
        {
            if (ToolsService.IsLocalAvailable(tool))
                continue;

            var toolProgress = new Progress<Percentage>(p =>
                Greeting = $"{tool} Progress: {p.Fraction:P}"
            );

            await ToolsService.DownloadAsync(tool, toolProgress, cancellationToken);

            var version = await ToolsService.GetVersionAsync(tool, cancellationToken);
            Greeting = $"Finished Download {tool} {version}";
        }

        var videoId = VideoId.Parse("https://www.youtube.com/watch?v=2dUykY2tHdA");
        var video = await YoutubeClient.Videos.GetAsync(videoId, cancellationToken);
        var streamManifest = await YoutubeClient.Videos.Streams.GetManifestAsync(
            videoId,
            cancellationToken
        );

        // Select best audio stream (highest bitrate)
        var audioStreamInfo = streamManifest
            .GetAudioStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate();

        // Select best video stream (1080p60 in this example)
        var videoStreamInfo = streamManifest
            .GetVideoStreams()
            .Where(s => s.Container == Container.Mp4)
            .First(s => s.VideoQuality.Label == "1080p60");

        var size = audioStreamInfo.Size.Bytes + videoStreamInfo.Size.Bytes;
        var progress = new DownloadProgress(
            size,
            args =>
            {
                Greeting = $"""
                    ETA: {args.Eta.Humanize()}
                    Speed: {args.Speed.Bytes().Humanize("#.##")}/s
                    Average Speed: {args.AverageSpeed.Bytes().Humanize("#.##")}/s
                    Progress: {args.Downloaded.Bytes().Humanize()}/{size.Bytes().Humanize()}
                    Percent: {args.Percentage.Fraction:P1}
                    """;
            }
        );
        await YoutubeClient.Videos.DownloadAsync(
            [audioStreamInfo, videoStreamInfo],
            new ConversionRequestBuilder(
                RakeDirectoryConsts.Tools.CombinePath($"{video.Title.Sanitize()}.mp4")
            )
                .SetContainer(Container.Mp4)
                .SetFFmpegPath(ToolsService.GetPath(Tool.FFmpeg))
                .SetPreset(ConversionPreset.Medium)
                .Build(),
            progress,
            cancellationToken
        );
    }
}
