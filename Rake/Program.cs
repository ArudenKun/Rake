using System;
using Avalonia;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Rake.Helpers;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.AsyncFile;
using Velopack;
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
    public static int Main(string[] args)
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

        var builder = BuildAvaloniaApp();

        try
        {
            var loggerFactory = LoggerFactory.Create(loggingBuilder =>
                loggingBuilder.ClearProviders().AddSerilog()
            );
            VelopackApp.Build().Run(loggerFactory.CreateLogger<VelopackApp>());
            return builder.StartWithClassicDesktopLifetime(args);
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
            // Clean up after application shutdown
            if (builder.Instance is IDisposable disposableApp)
                disposableApp.Dispose();

            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();

    public static bool IsDebug
#if DEBUG
        => true;
#else
        => false;
#endif
}
