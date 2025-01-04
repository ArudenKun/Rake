using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;

namespace Rake.Extensions;

public static class AvaloniaExtensions
{
    public static Window? TryGetMainWindow(this IApplicationLifetime lifetime) =>
        lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime
            ? desktopLifetime.MainWindow
            : null;

    public static TopLevel? TryGetTopLevel(this IApplicationLifetime lifetime) =>
        lifetime.TryGetMainWindow()
        ?? (lifetime as ISingleViewApplicationLifetime)?.MainView?.GetVisualRoot() as TopLevel;

    public static bool TryShutdown(this IApplicationLifetime lifetime, int exitCode = 0)
    {
        switch (lifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktopLifetime:
                return desktopLifetime.TryShutdown(exitCode);
            case IControlledApplicationLifetime controlledLifetime:
                controlledLifetime.Shutdown(exitCode);
                return true;
            default:
                return false;
        }
    }
}
