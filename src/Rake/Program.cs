using System.Windows;
using Dapplo.Microsoft.Extensions.Hosting.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rake.Core.Helpers;
using Rake.Generators.DependencyInjection;
using Rake.Services;
using Rake.Views;
using Serilog;
using Serilog.Enrichers.ClassName;
using Serilog.Events;
using Serilog.Sinks.FileEx;
using Velopack;
using Wpf.Ui;

namespace Rake;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddCore();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationWindow>(sp => sp.GetRequiredService<MainWindow>());
                services.AddSingleton<IWpfShell>(sp => sp.GetRequiredService<MainWindow>());
            })
            .ConfigureWpf(builder => builder.UseApplication<App>())
            .UseWpfLifetime()
#if DEBUG
            .UseEnvironment(Environments.Development)
#endif
            .UseSerilog(
                (context, configuration) =>
                {
                    #region Logging

                    const string logTemplate =
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {ClassName}] {Message:lj} {NewLine}{Exception}";
                    var logsPath = EnvironmentHelper.AppDataDirectory.JoinPath("logs");

                    configuration
                        .MinimumLevel.Is(
                            context.HostingEnvironment.IsDevelopment()
                                ? LogEventLevel.Debug
                                : LogEventLevel.Information
                        )
                        .WriteTo.Console(outputTemplate: logTemplate)
                        .WriteTo.Async(x =>
                            x.FileEx(
                                logsPath.JoinPath(
                                    $"logs{(context.HostingEnvironment.IsDevelopment() ? ".debug" : "")}.txt"
                                ),
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
                        .Enrich.WithClassName();

                    #endregion
                }
            )
            .UseConsoleLifetime()
            .Build();

        try
        {
            VelopackApp.Build().Run();
            host.Run();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "A Fatal Error Occured");
            throw;
        }
        finally
        {
            host.StopAsync();
            host.Dispose();
            Log.CloseAndFlush();
        }
    }
}
