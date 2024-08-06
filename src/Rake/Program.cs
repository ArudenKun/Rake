using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Avalonia;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rake.Core.Helpers;
using Rake.Generator.DependencyInjection;
using Rake.Services;
using Rake.Services.Caching;
using Serilog;
using Serilog.Enrichers.ClassName;
using Serilog.Events;
using Serilog.Sinks.FileEx;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace Rake;

public static class Program
{
    private static readonly ServiceProvider Services;
    private static readonly ILogger<App> Logger;

    static Program()
    {
        ConfigureLogging();
        var services = new ServiceCollection();

        services.AddSingleton<App>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IJsonTypeInfoResolver>(AppJsonContext.Default);
        services.AddSingleton(AppJsonContext.Default.Options);
        services.AddSingleton<IDistributedCache>(sp => new FileCache(
            new FileCacheOptions(EnvironmentHelper.AppDataPath.JoinPath("cache")),
            sp.GetRequiredService<ILogger<FileCache>>(),
            sp.GetRequiredService<JsonSerializerOptions>()
        ));
        services.AddSingleton<FileCacheImageLoader>();
        services.AddSingleton<IFusionCacheSerializer, FileCacheSerializer>();
        services
            .AddFusionCache()
            .WithDefaultEntryOptions(opt =>
                opt.SetDuration(TimeSpan.FromMinutes(5))
                    .SetFailSafe(true)
                    .SetFactoryTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30))
            )
            .TryWithAutoSetup();

        services.AddCore();
        services.AddLogging(builder => builder.ClearProviders().AddSerilog(dispose: true));

        Services = services.BuildServiceProvider();
        Logger = Services.GetRequiredService<ILogger<App>>();
    }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var app = BuildAvaloniaApp();

        try
        {
            Logger.LogInformation("App Started");
            app.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An Error Occured");
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
            .AfterSetup(_ => CefRuntimeLoader.Initialize(new CefSettings
            {
                RootCachePath = EnvironmentHelper.AppDataPath.JoinPath("cache", "chrome")
            }))
            .WithInterFont()
            .LogToTrace();

    #region Logging

    private static void ConfigureLogging()
    {
        const string logTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {ClassName}] {Message:lj} {NewLine}{Exception}";
        var logsPath = EnvironmentHelper.AppDataPath.JoinPath("logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(IsDebug ? LogEventLevel.Debug : LogEventLevel.Warning)
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