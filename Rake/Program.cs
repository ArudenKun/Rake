using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using H.Formatters;
using H.Pipes;
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
using Serilog.Core;
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
    public static void Main(string[] args)
    {
        var loggingLevelSwitch = ConfigureLogging();

        var builder = Host.CreateApplicationBuilder(args);

        // Logging
        builder.Services.AddSingleton(loggingLevelSwitch);
        builder.Services.AddSerilog(dispose: true);

        // Services
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<UpdateService>();
        builder.Services.AddSingleton<ViewModelFactory>();
        builder.Services.AddSingleton<IVelopackLocator>(sp =>
            VelopackLocator.GetDefault(sp.GetRequiredService<ILogger<IVelopackLocator>>())
        );
        builder.Services.AddFactory(_ => Downloader.CreateInstance(HttpHelper.HttpClient));

        // ViewModels
        builder.Services.AddViewModels();

        builder.ConfigureAvalonia<App>(appBuilder =>
            appBuilder.UsePlatformDetect().UseR3().LogToTrace()
        );
        builder.ConfigureSingleInstance(mutexBuilder =>
        {
            const string id = "86246EE0-6591-4FD5-871C-3759C5E185FE";
            const string pipeName = $"Rake.{id}.pipe";
            mutexBuilder.MutexId = id;
            mutexBuilder.WhenFirstInstance += async (sp, logger, token) =>
            {
                if (sp.GetRequiredService<SettingsService>().IsMultipleInstancesEnabled)
                {
                    return;
                }

                await using var server = new PipeServer<string>(
                    pipeName,
                    new SystemTextJsonNativeAotFormatter(GlobalJsonSerializerContext.Default)
                );

                server.MessageReceived += (_, message) =>
                {
                    if (message.Message is not "LOCKED")
                        return;
                    var mainWindow = Application.Current?.ApplicationLifetime?.TryGetMainWindow();
                    if (mainWindow is null)
                    {
                        logger.LogWarning("Could not find main window");
                        return;
                    }

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        var initialState = mainWindow.WindowState;
                        if (initialState is WindowState.Minimized)
                        {
                            initialState = WindowState.Normal;
                        }

                        mainWindow.WindowState = WindowState.Minimized;
                        mainWindow.WindowState = initialState;
                    });
                };
                logger.LogInformation("Starting pipe server");
                await server.StartAsync(token).ConfigureAwait(false);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Shutting down pipe server");
                }
            };
            mutexBuilder.WhenNotFirstInstance += async (sp, logger, token) =>
            {
                if (sp.GetRequiredService<SettingsService>().IsMultipleInstancesEnabled)
                {
                    return;
                }

                logger.LogWarning("Another instance is already running");
                await using var client = new PipeClient<string>(
                    pipeName,
                    new SystemTextJsonNativeAotFormatter(GlobalJsonSerializerContext.Default)
                );
                await client.ConnectAsync(token).ConfigureAwait(false);
                logger.LogWarning("Sending message");
                await client.WriteAsync("LOCKED", token).ConfigureAwait(false);
                logger.LogWarning("Sent message");
                if (Application.Current?.ApplicationLifetime?.TryShutdown(2) is not true)
                    Environment.Exit(2);
            };
        });

        var host = builder.Build();

        try
        {
            VelopackApp.Build().Run(host.Services.GetRequiredService<ILogger<VelopackApp>>());
            host.Run();
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
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    [PublicAPI]
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UsePlatformDetect().LogToTrace();

    private static LoggingLevelSwitch ConfigureLogging()
    {
        const string logTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {ClassName}] {Message:lj} {NewLine}{Exception}";

        var loggingLevelSwitch = new LoggingLevelSwitch(
            IsDebug ? LogEventLevel.Debug : LogEventLevel.Information
        );
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
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

        return loggingLevelSwitch;
    }

    public static bool IsDebug
#if DEBUG
        => true;
#else
        => false;
#endif
}
