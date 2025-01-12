using System.Threading.Tasks;
using SukiUI.Dialogs;

namespace Rake.Extensions;

public static class SukiDialogExtensions
{
    /// <inheritdoc cref="SukiDialogBuilder.TryShow"/>
    public static Task<bool> TryShowAsync(this SukiDialogBuilder dialogBuilder)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dialog = dialogBuilder.Dialog;
        var showResult = dialogBuilder.TryShow();
        dialogBuilder.Manager.OnDialogDismissed += OnDialogDismissed;
        return tcs.Task.ContinueWith(_ =>
        {
            dialogBuilder.Manager.OnDialogDismissed -= OnDialogDismissed;
            return showResult;
        });

        void OnDialogDismissed(object sender, SukiDialogManagerEventArgs args)
        {
            if (dialog.Equals(args.Dialog))
            {
                tcs.TrySetResult(showResult);
            }
        }
    }
}
