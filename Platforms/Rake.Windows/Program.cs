using Avalonia;
using Avalonia.WebView.Windows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Velopack;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rake.Windows;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        Bootstrap.Setup(
            builder,
            appBuilder => appBuilder.UseWin32().UseSkia().UseR3().UseWindowWebView().LogToTrace()
        );

        var host = builder.Build();

        try
        {
            VelopackApp.Build().Run(host.Services.GetRequiredService<ILogger<VelopackApp>>());
            host.Run();
        }
        catch (Exception ex)
        {
            _ = PInvoke.MessageBox(
                new HWND(0),
                ex.ToString(),
                "Fatal Error",
                MESSAGEBOX_STYLE.MB_ICONERROR
            );

            throw;
        }
        finally
        {
            host.Dispose();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    [PublicAPI]
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UseWin32().UseSkia().LogToTrace();
}
