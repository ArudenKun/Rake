using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Rake.Services;
using Rake.ViewModels.Abstractions;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Rake.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(
        DialogService dialogService,
        ToastService toastService,
        ILogger<MainWindowViewModel> logger
    )
    {
        DialogService = dialogService;
        ToastService = toastService;
        _logger = logger;
    }

    public DialogService DialogService { get; }
    public ToastService ToastService { get; }

    public string Title { get; } = "Rake";
    public string Greeting => "adas";

    [RelayCommand]
    private async Task ShowYesNoDialog()
    {
        // DialogService
        //     .Manager.CreateDialog()
        //     .WithTitle("Delete")
        //     .WithContent("Are you sure you want to delete this item?")
        //     .WithActionButton("Yes", _ => _logger.LogInformation("Result: {0}", true), true)
        //     .WithActionButton("No", _ => _logger.LogInformation("Result: {0}", true), true)
        //     .OfType(NotificationType.Information)
        //     .Dismiss()
        //     .ByClickingBackground()
        //     .TryShow();
        var result = await DialogService.YesNoAsync(
            "Delete",
            "Are you sure you want to delete this item?"
        );
        _logger.LogInformation("Result: {Result}", result);
    }
}
