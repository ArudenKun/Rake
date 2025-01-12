using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using SukiUI.Toasts;

namespace Rake.Extensions;

public static class SukiToastExtensions
{
    public static Task QueueAsync(this SukiToastBuilder toastBuilder)
    {
        var tcs = new TaskCompletionSource();
        var toast = toastBuilder.Queue();
        toastBuilder.Manager.OnToastDismissed += OnToastDismissed;
        return tcs.Task.ContinueWith(task =>
        {
            toastBuilder.Manager.OnToastDismissed -= OnToastDismissed;
            return task;
        });

        void OnToastDismissed(object? sender, SukiToastManagerEventArgs args)
        {
            if (toast.Equals(args.Toast))
            {
                tcs.TrySetResult();
            }
        }
    }

    public static SukiToastBuilder WithActionButton(
        this SukiToastBuilder builder,
        object buttonContent,
        bool dismissOnClick = false
    ) => builder.WithActionButton(buttonContent, _ => { }, dismissOnClick);

    public static SukiToastBuilder WithActionButtonNormal(
        this SukiToastBuilder builder,
        object buttonContent,
        bool dismissOnClick = false
    )
    {
        builder.AddActionButton(buttonContent, _ => { }, dismissOnClick, false);
        return builder;
    }

    public static SukiToastBuilder After(
        this SukiToastBuilder.DismissToast dismissToast,
        int seconds
    ) => dismissToast.After(TimeSpan.FromSeconds(seconds));

    public static SukiToastBuilder After(
        this SukiToastBuilder.DismissToast dismissToast,
        long milliseconds
    ) => dismissToast.After(TimeSpan.FromMilliseconds(milliseconds));

    public static SukiToastBuilder CreateSimpleToast(
        this ISukiToastManager manager,
        NotificationType notificationType = NotificationType.Information
    ) =>
        manager
            .CreateToast()
            .OfType(notificationType)
            .Dismiss()
            .After(TimeSpan.FromSeconds(3.0))
            .Dismiss()
            .ByClicking();
}
