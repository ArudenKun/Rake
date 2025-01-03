using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Microsoft.Extensions.Logging;
using SukiUI.Dialogs;

namespace Rake.Services;

public class DialogService
{
    private readonly ILogger<DialogService> _logger;

    public DialogService(ISukiDialogManager manager, ILogger<DialogService> logger)
    {
        Manager = manager;
        _logger = logger;
    }

    public ISukiDialogManager Manager { get; }

    public Task<bool> YesNoAsync(
        string title,
        object content,
        bool dismissOnClick = true,
        NotificationType notificationType = NotificationType.Information
    )
    {
        var tcs = new TaskCompletionSource<bool>();
        Manager
            .CreateDialog()
            .WithTitle(title)
            .WithContent(content)
            .WithActionButton("Yes", _ => tcs.SetResult(true), dismissOnClick)
            .WithActionButton("No", _ => tcs.SetResult(false), dismissOnClick)
            .OfType(notificationType)
            .Dismiss()
            .ByClickingBackground()
            .TryShow();
        return tcs.Task;
    }
}
