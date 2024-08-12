using System;
using System.Linq;
using Avalonia;
using Serilog;
using Windows.Win32;

namespace Rake.Windows;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var showConsole = args.Contains("--console");

        try
        {
            if (showConsole)
            {
                PInvoke.AllocConsole();
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.CloseAndFlush();

            if (showConsole)
            {
                PInvoke.FreeConsole();
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
