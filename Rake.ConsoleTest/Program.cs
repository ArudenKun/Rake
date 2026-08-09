using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Rake.Core;
using Rake.Core.Twitch;
using Rake.Core.Twitch.Videos;
using Serilog;
using Spectre.Console;
using Volo.Abp;

namespace Rake.ConsoleTest;

public static class Program
{
    private const string TestClipId = "LightPlumpJalapenoTheThing-ulv8xDxhKxavWdlW";
    private const string TestClipUrl = "https://www.twitch.tv/braxophone/clip/" + TestClipId;

    private const string TestVideoId = "2772293749";
    private const string TestVideoUrl = "https://www.twitch.tv/videos/" + TestVideoId;

    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger();

        AnsiConsole.Write(new Rule("[yellow]Twitch Clip Downloader Test[/]").LeftJustified());

        var application = await AbpApplicationFactory.CreateAsync<RakeConsoleTestModule>(options =>
        {
            options.UseAutofac();
            options.Services.AddLogging(lb => lb.AddSerilog(dispose: true));
        });

        await application.InitializeAsync();

        try
        {
            var toolsService = application.ServiceProvider.GetRequiredService<IToolsService>();
            foreach (var tool in Enum.GetValues<Tool>())
            {
                if (toolsService.IsLocalAvailable(tool))
                    continue;

                AnsiConsole.MarkupLine($"[grey]Downloading tool:[/] [cyan]{tool}[/]");
                await toolsService.DownloadAsync(tool);

                var version = await toolsService.GetVersionAsync(tool);
                AnsiConsole.MarkupLine($"[green]Downloaded {tool} {version}[/]");
            }

            var twitchClient = application.ServiceProvider.GetRequiredService<TwitchClient>();

            // TwitchId clipId = TestClipUrl;
            TwitchId videoId = TestVideoUrl;
            AnsiConsole.MarkupLine(
                $"[bold]Parsing Twitch ID:[/] [blue]{videoId}[/] (IsClip: [green]{videoId.IsClip}[/], IsVideo: [red]{videoId.IsVideo}[/])"
            );

            AnsiConsole.MarkupLine("[grey]Fetching metadata...[/]");
            // var clip = await twitchClient.GetClipAsync(clipId);
            var video = await twitchClient.GetVideoAsync(videoId);

            // Display clip info panel
            var panel = new Panel(
                new Markup(
                    $"[bold]Title:[/] {Markup.Escape(video.Title)}\n"
                        + $"[bold]Broadcaster:[/] {Markup.Escape(video.Owner.DisplayName)}\n"
                        + $"[bold]Game:[/] {Markup.Escape(video.Game.DisplayName)}\n"
                        + $"[bold]Duration:[/] {TimeSpan.FromSeconds(video.DurationSeconds):mm\\:ss}\n"
                        + $"[bold]Views:[/] {video.Views:N0}"
                )
            )
            {
                Header = new PanelHeader("[yellow]Clip Metadata[/]"),
                Border = BoxBorder.Rounded,
            };
            AnsiConsole.Write(panel);

            // Create Streams Table
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold cyan]Available Video Qualities[/]")
                .AddColumn(new TableColumn("[bold]Quality Name[/]"))
                .AddColumn(new TableColumn("[bold]FPS[/]"))
                .AddColumn(new TableColumn("[bold]Bitrate[/]"))
                .AddColumn(new TableColumn("[bold]Estimated Size[/]"));

            foreach (var quality in video.Qualities)
            {
                table.AddRow(
                    quality.Name,
                    quality.Fps?.ToString() ?? "N/A",
                    $"{quality.Bitrate / 1000:N0} kbps",
                    quality.FileSize.Humanize()
                );
            }

            AnsiConsole.Write(table);

            // Prompt user to select stream quality interactively
            var selectedQuality = AnsiConsole.Prompt(
                new SelectionPrompt<TwitchVideoQuality>()
                    .Title("\n[yellow]Select the stream quality to download:[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more streams)[/]")
                    .UseConverter(s =>
                        $"{s.Name}, {s.Fps?.ToString() ?? "N/A"}fps, {s.Bitrate / 1000:N0} kbps, ~{s.FileSize.Humanize()})"
                    )
                    .AddChoices(video.Qualities)
            );

            AnsiConsole.MarkupLine(
                $"[bold yellow]Selected quality:[/] [green]{selectedQuality.Name}[/], ~{selectedQuality.FileSize.Humanize()})\n"
            );

            var outputPath = Path.Combine(
                AppContext.BaseDirectory,
                "Tools",
                $"test_clip_{selectedQuality.Name}_{Guid.CreateVersion7():N}.mp4"
            );

            // Download using Spectre Console Progress

            // await AnsiConsole
            //     .Progress()
            //     .AutoClear(false)
            //     .HideCompleted(false)
            //     .Columns([
            //         new TaskDescriptionColumn(),
            //         new ProgressBarColumn(),
            //         new PercentageColumn(),
            //         new SpinnerColumn(),
            //     ])
            //     .StartAsync(async ctx =>
            //     {
            //         var downloadTask = ctx.AddTask("[green]Downloading Clip[/]", maxValue: 100);
            //         var encodingTask = ctx.AddTask("[blue]Encoding Metadata[/]", maxValue: 100);
            //         encodingTask.IsIndeterminate = true;
            //         await twitchClient.DownloadClipAsync(
            //             clipId,
            //             outputPath,
            //             options =>
            //                 options
            //                     .WithQuality(selectedQuality.Name)
            //                     .OnDownloadStarted(() =>
            //                     {
            //                         downloadTask.Value = 0;
            //                     })
            //                     .OnDownloadProgress(args =>
            //                     {
            //                         downloadTask.Value = args.Percentage.Value;
            //                     })
            //                     .OnDownloadCompleted(() =>
            //                     {
            //                         downloadTask.Value = 100;
            //                     })
            //                     .OnEncodingStarted(() =>
            //                     {
            //                         encodingTask.IsIndeterminate = false;
            //                         encodingTask.Value = 0;
            //                     })
            //                     .OnEncodingProgress(percentage =>
            //                     {
            //                         encodingTask.Value = percentage;
            //                     })
            //                     .OnEncodingCompleted(() =>
            //                     {
            //                         encodingTask.Value = 100;
            //                     })
            //         );
            //     });

            var progress = new ConsoleProgress();
            await twitchClient.DownloadVideoAsync(
                videoId,
                outputPath,
                options =>
                    options
                        .WithQuality(selectedQuality)
                        .OnDownloadStarted(() =>
                        {
                            AnsiConsole.WriteLine("Download Started");
                        })
                        .OnDownloadProgress(progress.Report)
                        .OnDownloadCompleted(() =>
                        {
                            AnsiConsole.WriteLine("Download Completed");
                        })
            );

            AnsiConsole.MarkupLine(
                $"\n[bold green]Download complete![/] Saved clip to: {outputPath}"
            );
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
            throw;
        }
        finally
        {
            await application.ShutdownAsync();
            application.Dispose();
            await Log.CloseAndFlushAsync();
        }
    }
}
