using System;
using AutoInterfaceAttributes;
using Avalonia.Threading;
using Material.Styles.Controls;
using Material.Styles.Models;

namespace Rake.Services;

[AutoInterface]
public sealed class SnackbarService : ISnackbarService
{
    private readonly TimeSpan _defaultDuration = TimeSpan.FromSeconds(5);

    public void Notify(string message, TimeSpan? duration = null) =>
        SnackbarHost.Post(
            new SnackbarModel(message, duration ?? _defaultDuration),
            null,
            DispatcherPriority.Normal
        );

    public void Notify(
        string message,
        string actionText,
        Action actionHandler,
        TimeSpan? duration = null
    ) =>
        SnackbarHost.Post(
            new SnackbarModel(
                message,
                duration ?? _defaultDuration,
                new SnackbarButtonModel { Text = actionText, Action = actionHandler }
            ),
            null,
            DispatcherPriority.Normal
        );
}
