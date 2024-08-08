using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;

namespace Rake.Extensions;

public static class AvaloniaExtensions
{
    public static Window? TryGetMainWindow(this IApplicationLifetime lifetime)
    {
        return lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime
            ? desktopLifetime.MainWindow
            : null;
    }

    public static Window GetMainWindow(this IApplicationLifetime lifetime)
    {
        return lifetime.TryGetMainWindow()
            ?? throw new ApplicationException("Could not find the main window.");
    }

    public static TopLevel? TryGetTopLevel(this IApplicationLifetime lifetime)
    {
        return lifetime.TryGetMainWindow()
            ?? (lifetime as ISingleViewApplicationLifetime)?.MainView?.GetVisualRoot() as TopLevel;
    }

    public static TopLevel GetTopLevel(this IApplicationLifetime lifetime)
    {
        return lifetime.TryGetTopLevel()
            ?? throw new ApplicationException("Could not find the top-level visual element.");
    }

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
