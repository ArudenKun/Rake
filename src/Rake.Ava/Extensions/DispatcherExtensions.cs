using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Rake.Extensions;

internal static class DispatcherExtensions
{
    public static Task<T> PostAsync<T>(
        this IDispatcher dispatcher,
        Func<T> action,
        DispatcherPriority dispatcherPriority = default
    )
    {
        var completion = new TaskCompletionSource<T>();
        dispatcher.Post(() => completion.SetResult(action()), dispatcherPriority);
        return completion.Task;
    }

    public static void WaitOnDispatcherFrame(this Task task, Dispatcher? dispatcher = null)
    {
        var frame = new DispatcherFrame();
        AggregateException? capturedException = null;

        task.ContinueWith(
            t =>
            {
                capturedException = t.Exception;
                frame.Continue = false;
            },
            TaskContinuationOptions.AttachedToParent
        );

        dispatcher ??= Dispatcher.UIThread;
        dispatcher.PushFrame(frame);

        if (capturedException != null)
        {
            throw capturedException;
        }
    }
}
