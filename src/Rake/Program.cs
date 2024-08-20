using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Avalonia;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rake.Core.Helpers;
using Rake.Generators.DependencyInjection;
using Rake.Services;
using Rake.Services.Caching;
using Serilog;
using Serilog.Enrichers.ClassName;
using Serilog.Events;
using Serilog.Sinks.FileEx;
using Velopack;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace Rake;

public static class Program
{
    private static readonly ServiceProvider Services;
    private static readonly ILogger<App> Logger;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var app = BuildAvaloniaApp();

        try
        {
            VelopackApp.Build().Run(Services.GetRequiredService<ILogger<VelopackApp>>());
            Logger.LogInformation("App Started");
            app.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Fatal Error");
            if (OperatingSystem.IsWindowsVersionAtLeast(5))
            {
                _ = PInvoke.MessageBox(
                    (HWND)0,
                    e.ToString(),
                    "Fatal Error",
                    MESSAGEBOX_STYLE.MB_ICONERROR
                );
            }
            throw;
        }
        finally
        {
            Services.Dispose();
            Logger.LogInformation("App Exited");
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure(() => Services.GetRequiredService<App>())
            .UsePlatformDetect()
            .LogToTrace();

    static Program()
    {
        ConfigureLogging();
        var services = new ServiceCollection();

        services.AddSingleton<App>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IViewFactory, ViewFactory>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IJsonTypeInfoResolver>(AppJsonContext.Default);
        services.AddSingleton(AppJsonContext.Default.Options);
        services.AddSingleton<IDistributedCache>(sp => new FileCache(
            new FileCacheOptions(EnvironmentHelper.AppDataDirectory.JoinPath("cache", "file")),
            sp.GetRequiredService<ILogger<FileCache>>(),
            sp.GetRequiredService<JsonSerializerOptions>()
        ));
        services.AddSingleton<FileCacheImageLoader>();
        services.AddSingleton<IFusionCacheSerializer, FileCacheSerializer>();
        services
            .AddFusionCache()
            .WithDefaultEntryOptions(options =>
                options
                    .SetDuration(TimeSpan.FromMinutes(5))
                    .SetFailSafe(true)
                    .SetFactoryTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30))
            )
            .TryWithAutoSetup();

        services.AddGenerated();
        services.AddLogging(builder => builder.ClearProviders().AddSerilog(dispose: true));

        Services = services.BuildServiceProvider();
        Logger = Services.GetRequiredService<ILogger<App>>();
    }

    #region Logging

    private static void ConfigureLogging()
    {
        const string logTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {ClassName}] {Message:lj} {NewLine}{Exception}";
        var logsPath = EnvironmentHelper.AppDataDirectory.JoinPath("logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(IsDebug ? LogEventLevel.Debug : LogEventLevel.Information)
            .WriteTo.Console(outputTemplate: logTemplate)
            .WriteTo.Async(x =>
                x.FileEx(
                    logsPath.JoinPath($"logs{(IsDebug ? ".debug" : "")}.txt"),
                    ".dd-MM-yyyy",
                    outputTemplate: logTemplate,
                    rollingInterval: RollingInterval.Day,
                    rollOnEachProcessRun: false,
                    rollOnFileSizeLimit: true,
                    preserveLogFileName: true,
                    shared: true
                )
            )
            .WriteTo.FileEx(
                logsPath.JoinPath("logs.error.txt"),
                outputTemplate: logTemplate,
                rollingInterval: RollingInterval.Day,
                rollOnEachProcessRun: false,
                rollOnFileSizeLimit: true,
                preserveLogFileName: true,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Fatal
            )
            .Enrich.FromLogContext()
            .Enrich.WithClassName()
            .CreateLogger();
    }

    private static bool IsDebug
#if DEBUG
        => true;
#else
        => false;
#endif

    #endregion
}
