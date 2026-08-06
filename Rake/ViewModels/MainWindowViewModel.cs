using System;
using System.Buffers;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using Microsoft.Extensions.Logging;
using R3;
using R3.ObservableEvents;
using Rake.Core;
using Rake.Core.Extensions;
using Rake.Core.YtDlp;

namespace Rake.ViewModels;

public partial class MainWindowViewModel : ViewModel
{
    public MainWindowViewModel(IToolsService toolsService)
    {
        ToolsService = toolsService;
    }

    protected IToolsService ToolsService { get; }

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [RelayCommand]
    private async Task ShowExceptionAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("QuickJs Path: {Path}", RakePathConsts.QuickJs);
        Logger.LogInformation("FFmpeg Path: {Path}", RakePathConsts.FFmpeg);
        Logger.LogInformation("yt-dlp Path: {Path}", RakePathConsts.YtDlp);
        Logger.LogInformation("Aria2 Path: {Path}", RakePathConsts.Aria2);

        foreach (var tool in Enum.GetValues<Tool>().Where(tool => tool is not Tool.Deno))
        {
            if (ToolsService.IsLocalAvailable(tool))
                continue;

            var progress = new Progress<Percentage>(p =>
                Greeting = $"{tool} Progress: {p.Fraction:P}"
            );

            await ToolsService.DownloadAsync(tool, progress, cancellationToken);

            var version = await ToolsService.GetVersionAsync(tool, cancellationToken);
            Greeting = $"Finished Download {tool} {version}";
        }

        var url = "https://www.youtube.com/watch?v=8j4EgU75tJ8";
        var ytDlp = new YtDlp(RakePathConsts.YtDlp, LoggerFactory.CreateLogger<YtDlp>())
            .WithDefaults()
            .WithBestVideoPlusBestAudio()
            .WithConcurrentFragments()
            .WithWindowsFilenames()
            .WithMkvOutput()
            .WithAria2()
            .WithOutputTemplate("%(upload_date>%Y-%m-%d)s - %(title).90s [%(resolution)s].%(ext)s")
            .WithOutputFolder(RakeDirectoryConsts.Tools);

        var ytDlpEvents = ytDlp.Events();
        var getMetadataTasks = ytDlp.GetMetadataAsync(url, cancellationToken);
        var getFormatsTask = ytDlp.GetFormatsAsync(url, cancellationToken);

        await Task.WhenAll(getMetadataTasks, getFormatsTask);

        var metadata = getMetadataTasks.Result;
        var formats = getFormatsTask.Result;

        DownloadProgressState lastState = default;

        var title = metadata?.Title ?? "Unknown";
        var downloadProgress = new Progress<DownloadProgressState>(state =>
        {
            Greeting = string.Create(
                CultureInfo.InvariantCulture,
                $"""
                Title: {title}
                ETA: {state.Eta}
                Size: {state.Size}
                Speed: {state.Speed}
                Fragments: {state.Fragments}
                Progress: {state.Progress.Fraction:P1}
                """
            );
        }).WithDeduplication().WithOrdering();

        var bag = new DisposableBag();
        ytDlpEvents
            .ProgressDownload.ObserveOnUIThreadDispatcher()
            .Subscribe(args =>
            {
                var percentage = Percentage.FromValue(args.Percent);
                lastState = new DownloadProgressState(
                    percentage,
                    args.ETA,
                    args.Size,
                    args.Speed,
                    args.Fragments
                );
                downloadProgress.Report(lastState);
            })
            .AddTo(ref bag);

        try
        {
            await ytDlp.DownloadAsync(url, cancellationToken);
            var finalState = lastState with { Progress = Percentage.FromFraction(1.0) };
            downloadProgress.Report(finalState);
        }
        finally
        {
            bag.Dispose();
        }
    }

    public readonly record struct DownloadProgressState(
        Percentage Progress,
        string Eta,
        string Size,
        string Speed,
        string Fragments
    ) : IComparable<DownloadProgressState>
    {
        public int CompareTo(DownloadProgressState other) =>
            Progress.Fraction.CompareTo(other.Progress.Fraction);
    }
}
