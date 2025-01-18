using System.Runtime.Versioning;
using Avalonia;
using Avalonia.WebView.Linux;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Velopack;

namespace Rake.Linux;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    [SupportedOSPlatform("linux")]
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        Bootstrap.Setup(
            builder,
            appBuilder => appBuilder.UseX11().UseSkia().UseR3().UseLinuxWebView(false).LogToTrace()
        );

        var host = builder.Build();

        try
        {
            VelopackApp.Build().Run(host.Services.GetRequiredService<ILogger<VelopackApp>>());
            host.Run();
        }
        finally
        {
            host.Dispose();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    [PublicAPI]
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UseX11().UseSkia().LogToTrace();
}
