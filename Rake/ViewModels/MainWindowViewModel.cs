using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Gress;
using Microsoft.Extensions.Logging;
using Rake.Extensions;
using Rake.Services;
using Rake.ViewModels.Abstractions;
using SukiUI.Toasts;
using Velopack.Locators;

namespace Rake.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly UpdateService _updateService;
    private readonly Dispatcher _dispatcher;
    private readonly SettingsService _settingsService;
    private readonly IVelopackLocator _velopackLocator;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        UpdateService updateService,
        Dispatcher dispatcher,
        SettingsService settingsService,
        IVelopackLocator velopackLocator
    )
    {
        _logger = logger;
        _updateService = updateService;
        _dispatcher = dispatcher;
        _settingsService = settingsService;
        _velopackLocator = velopackLocator;
    }

    public string Greeting =>
        _velopackLocator.CurrentlyInstalledVersion?.ToNormalizedString() ?? "Test";

    protected override async Task OnLoadedAsync()
    {
        _logger.LogInformation("Checking for updates");
        await CheckForUpdatesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settingsService.Save();
            _updateService.FinalizeUpdate();
        }

        base.Dispose(disposing);
    }

    [RelayCommand]
    private async Task ShowYesNoDialog()
    {
        await ToastManager
            .CreateSimpleInfoToast()
            .WithTitle("Test")
            .WithContent("Test content")
            .QueueAndWaitAsync();

        _logger.LogInformation("Showing YesNoDialog");
    }

    private async Task CheckForUpdatesAsync()
    {
        ToastManager
            .CreateSimpleInfoToast()
            .WithTitle("Checking For Updates")
            .WithContent("Updates Found")
            .Queue();
        try
        {
            var updateInfo = await _updateService.CheckForUpdatesAsync();
            if (updateInfo is null)
                return;

            _logger.LogInformation("Update found {0}", updateInfo.TargetFullRelease.Version);

            await ToastManager
                .CreateToast()
                .WithTitle("Update Found")
                .WithContent($"Preparing to Download {updateInfo.TargetFullRelease.Version}")
                .Dismiss()
                .After(TimeSpan.FromSeconds(2))
                .QueueAndWaitAsync()
                .ConfigureAwait(false);

            var progressBar = new ProgressBar { Value = 0, ShowProgressText = true };
            var progress = new Progress<Percentage>(percentage =>
                progressBar.Value = percentage.Value
            );
            var downloadToast = ToastManager
                .CreateToast()
                .WithTitle("Downloading Update")
                .WithContent(progressBar)
                .Queue();

            await _dispatcher.Invoke(async () =>
            {
                await _updateService
                    .PrepareUpdatesAsync(updateInfo, progress)
                    .ConfigureAwait(false);
                ToastManager.Dismiss(downloadToast);
            });

            ToastManager
                .CreateToast()
                .WithTitle("Download Finished")
                .WithContent($"Update {updateInfo.TargetFullRelease.Version} has been downloaded")
                .WithActionButtonNormal("Later", _ => { })
                .WithActionButton(
                    "Now",
                    _ =>
                    {
                        _updateService.FinalizeUpdate();
                        if (Application.Current?.ApplicationLifetime?.TryShutdown(2) != true)
                            Environment.Exit(2);
                    }
                )
                .Queue();

            OnPropertyChanged(new PropertyChangedEventArgs(Greeting));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Update Failed");
            ToastManager
                .CreateToast()
                .WithTitle("Error")
                .WithContent("An Error Occured While Performing the Update")
                .OfType(NotificationType.Error)
                .Queue();
        }
    }
}
