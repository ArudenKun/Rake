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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Rake.Data;
using Rake.Extensions;
using Rake.Helpers;
using Rake.Hosting;
using Rake.Services;
using Rake.Utilities.Downloading;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.AsyncFile;
using Velopack.Locators;

namespace Rake;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public static class Bootstrap
{
    public static void Setup(IHostApplicationBuilder builder, Action<AppBuilder> configure)
    {
        var loggingLevelSwitch = ConfigureLogging();

        // Logging
        builder.Services.AddSingleton(loggingLevelSwitch);
        builder.Services.AddSerilog(dispose: true);

        // ViewModels
        builder.Services.AddViewModels();

        // Services
        builder.Services.AddSingleton<SqliteDatabase>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<UpdateService>();
        builder.Services.AddSingleton<ViewModelFactory>();
        builder.Services.AddSingleton<IVelopackLocator>(sp =>
            VelopackLocator.GetDefault(sp.GetRequiredService<ILogger<IVelopackLocator>>())
        );
        builder.Services.AddFactory(_ => Downloader.CreateInstance(HttpHelper.HttpClient));
        builder.Services.AddHostedService<StartupService>();

        builder.Services.AddQuartz(options =>
        {
            options.UsePersistentStore(storeOptions =>
            {
                storeOptions.UseMicrosoftSQLite(providerOptions =>
                {
                    providerOptions.ConnectionString = PathHelper.DatabaseConnectionString;
                });
                storeOptions.UseProperties = true;
                storeOptions.UseSystemTextJsonSerializer();
            });
        });
        builder.Services.AddQuartzHostedService(options =>
        {
            options.AwaitApplicationStarted = true;
            options.WaitForJobsToComplete = true;
        });

        builder.ConfigureAvalonia<App>(configure);
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
                logger.LogDebug("Starting pipe server");
                await server.StartAsync(token).ConfigureAwait(false);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug("Shutting down pipe server");
                }
            };
            mutexBuilder.WhenNotFirstInstance += async (sp, logger, token) =>
            {
                if (sp.GetRequiredService<SettingsService>().IsMultipleInstancesEnabled)
                {
                    return;
                }

                logger.LogInformation("Another instance is already running");
                await using var client = new PipeClient<string>(
                    pipeName,
                    new SystemTextJsonNativeAotFormatter(GlobalJsonSerializerContext.Default)
                );
                await client.ConnectAsync(token).ConfigureAwait(false);
                logger.LogDebug("Sending message");
                await client.WriteAsync("LOCKED", token).ConfigureAwait(false);
                logger.LogDebug("Sent message");
                if (Application.Current?.ApplicationLifetime?.TryShutdown(2) is not true)
                    Environment.Exit(2);
            };
        });
    }

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

    private static bool IsDebug
#if DEBUG
        => true;
#else
        => false;
#endif
}
