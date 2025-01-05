using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rake.Core;
using Rake.Core.Downloading;
using Rake.Core.Helpers;
using Rake.Extensions;
using Rake.Hosting;
using Rake.Services;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.AsyncFile;
using Velopack;
using Velopack.Locators;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rake;

[PublicAPI]
public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public static async Task Main(string[] args)
    {
        ConfigureLogging();
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSerilog(dispose: true);
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<UpdateService>();
        builder.Services.AddSingleton<IVelopackLocator>(sp =>
            VelopackLocator.GetDefault(sp.GetRequiredService<ILogger<IVelopackLocator>>())
        );
        builder.Services.AddSingleton(_ => Dispatcher.UIThread);
        builder.Services.AddFactory(_ => Downloader.CreateInstance(HttpHelper.HttpClient));
        builder.Services.AddViewModels();
        builder.ConfigureAvalonia<App>(appBuilder => appBuilder.UsePlatformDetect().LogToTrace());

        var host = builder.Build();
        try
        {
            VelopackApp.Build().Run(host.Services.GetRequiredService<ILogger<VelopackApp>>());
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(5))
            {
                _ = PInvoke.MessageBox(
                    (HWND)0,
                    ex.ToString(),
                    "Fatal Error",
                    MESSAGEBOX_STYLE.MB_ICONERROR
                );
            }
            throw;
        }
        finally
        {
            host.Dispose();
            await Log.CloseAndFlushAsync();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    [PublicAPI]
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UsePlatformDetect().LogToTrace();

    private static void ConfigureLogging()
    {
        const string logTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {ClassName}] {Message:lj} {NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(IsDebug ? LogEventLevel.Debug : LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithClassName()
            .WriteTo.Console(outputTemplate: logTemplate)
            .WriteTo.AsyncFile(
                PathHelper.LogsDirectory.Combine("log.txt"),
                logTemplate,
                new RollingPolicyOptions
                {
                    FileSizeLimitBytes = (int)100.Megabytes().Bytes,
                    RollingRetentionDays = 30,
                    RollOnStartup = true,
                }
            )
            .CreateLogger();
    }

    public static bool IsDebug
#if DEBUG
        => true;
#else
        => false;
#endif
}
