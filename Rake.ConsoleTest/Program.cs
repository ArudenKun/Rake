using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Rake.Core;
using Rake.Core.Twitch;
using Serilog;
using Volo.Abp;

namespace Rake.ConsoleTest;

public static class Program
{
    private const string TestClipId = "LightPlumpJalapenoTheThing-ulv8xDxhKxavWdlW";
    private const string TestClipUrl = "https://www.twitch.tv/braxophone/clip/" + TestClipId;
    private const string TestVideoId = "2772293749";
    private const string TestVideoUrl = "https://www.twitch.tv/videos/" + TestVideoId;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(
        JsonSerializerDefaults.General
    )
    {
        WriteIndented = true,
    };

    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger();

        TwitchId id = TestVideoUrl;
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

                await toolsService.DownloadAsync(tool);

                var version = await toolsService.GetVersionAsync(tool);
                Console.WriteLine($"Finished Download {tool} {version}");
            }

            var twitchClient = application.ServiceProvider.GetRequiredService<TwitchClient>();
            var video = await twitchClient.GetAsync(id);
            // var stream = video.Streams.First();
            // Console.WriteLine($"Parsed Length: {ByteSize.Parse($"{stream.FileSize}").Humanize()}");
            Console.WriteLine(JsonSerializer.Serialize(video, JsonSerializerOptions));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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
