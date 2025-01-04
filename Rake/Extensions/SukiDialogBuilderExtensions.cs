using System.Threading;
using System.Threading.Tasks;
using SukiUI.Dialogs;

namespace Rake.Extensions;

public static class SukiDialogBuilderExtensions
{
    public static SukiDialogBuilder WithActionButton(
        this SukiDialogBuilder builder,
        object content,
        bool dismissOnClick = false,
        params string[] classes
    ) => builder.WithActionButton(content, _ => { }, dismissOnClick, classes);

    public static Task WaitAsync(
        this SukiDialogBuilder builder,
        CancellationToken cancellationToken = default
    )
    {
        var tcs = new TaskCompletionSource();
        builder.OnDismissed(_ => tcs.SetResult());
        return tcs.Task;
    }
}
