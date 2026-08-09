using System;
using System.Threading.Tasks;
using Autofac;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Rake.Hosting;
using Rake.Logging;
using Serilog;
using Serilog.Events;
using Velopack;
using Volo.Abp.Autofac;

namespace Rake;

public static class Program
{
    private const string LogOutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(c => c.File(RakePathConsts.Logs, outputTemplate: LogOutputTemplate))
            .WriteTo.Async(c => c.Console(outputTemplate: LogOutputTemplate))
            .CreateBootstrapLogger();

        VelopackApp.Build().SetLogger(new SerilogVelopackLogger()).Run();

        var logger = Log.ForContext<IHost>();
        try
        {
            logger.Information("Starting Starward");
            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile(RakePathConsts.Setting, true);
            builder.ConfigureContainer(
                new AbpAutofacServiceProviderFactory(new ContainerBuilder())
            );
            builder.Services.AddSerilog(
                (services, loggerConfiguration) =>
                    loggerConfiguration
                        .ReadFrom.Services(services)
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override(
                            "Microsoft.EntityFrameworkCore",
                            LogEventLevel.Warning
                        )
                        .WriteTo.Async(c =>
                            c.File(RakePathConsts.Logs, outputTemplate: LogOutputTemplate)
                        )
                        .WriteTo.Async(c => c.Console(outputTemplate: LogOutputTemplate))
                        .Enrich.FromLogContext()
                        .Enrich.WithDemystifiedStackTraces()
            );
            await builder.AddApplicationAsync<App, RakeModule>(appBuilder =>
                appBuilder
                    .UseR3(ex => Log.Error(ex, "[R3] Unhandled Exception"))
                    .UsePlatformDetect()
#if DEBUG
                    .WithDeveloperTools()
#endif
                    .LogToTrace()
                    .LogToSerilog()
            );
            var host = builder.Build();
            await host.InitializeApplicationAsync();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            logger.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
