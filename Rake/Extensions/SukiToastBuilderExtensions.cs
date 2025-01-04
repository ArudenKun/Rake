using System.Threading;
using System.Threading.Tasks;
using SukiUI.Toasts;

namespace Rake.Extensions;

public static class SukiHostExtensions
{
    public static SukiToastBuilder WithActionButton(
        this SukiToastBuilder builder,
        object content,
        bool dismissOnClick = false
    ) => builder.WithActionButton(content, _ => { }, dismissOnClick);

    public static Task QueueAndWaitAsync(
        this SukiToastBuilder builder,
        CancellationToken cancellationToken = default
    )
    {
        var tcs = new TaskCompletionSource();
        builder.OnDismissed(_ => tcs.SetResult());
        builder.Queue();
        return tcs.Task;
    }
}
