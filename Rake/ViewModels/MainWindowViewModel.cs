using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Rake.Services;
using Rake.ViewModels.Abstractions;
using Velopack.Locators;

namespace Rake.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly UpdateService _updateService;
    private readonly SettingsService _settingsService;
    private readonly IVelopackLocator _velopackLocator;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        UpdateService updateService,
        SettingsService settingsService,
        IVelopackLocator velopackLocator
    )
    {
        _logger = logger;
        _updateService = updateService;
        _settingsService = settingsService;
        _velopackLocator = velopackLocator;
    }

    public string Greeting =>
        _velopackLocator.CurrentlyInstalledVersion?.ToNormalizedString() ?? "Test";

    protected override async Task OnLoadedAsync()
    {
        // await CheckForUpdatesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settingsService.Save();
            _updateService.FinalizeUpdate(false);
        }

        base.Dispose(disposing);
    }

    [RelayCommand]
    private async Task ShowYesNoDialog()
    {
        // .CreateSimpleInfoToast()
        // .WithTitle("Test")
        // .WithContent("Test content")
        // .Dismiss()
        // .ByClicking()
        // .QueueAndWaitAsync();

        _logger.LogInformation("Finished Showing YesNoDialog");
    }

    // private async Task CheckForUpdatesAsync()
    // {
    //     try
    //     {
    //         _logger.LogInformation("Checking for updates");
    //
    //         await ToastManager
    //             .CreateSimpleInfoToast()
    //             .WithTitle("Update")
    //             .WithContent("Checking for Updates")
    //             .QueueAndWaitAsync();
    //
    //         var updateInfo = await _updateService.CheckForUpdatesAsync();
    //         if (updateInfo is null)
    //         {
    //             ToastManager
    //                 .CreateSimpleInfoToast()
    //                 .WithTitle("Update")
    //                 .WithContent("No Updates Found")
    //                 .Queue();
    //
    //             return;
    //         }
    //
    //         _logger.LogInformation("Update Found {0}", updateInfo.TargetFullRelease.Version);
    //
    //         await ToastManager
    //             .CreateToast()
    //             .WithTitle("Update Found")
    //             .WithContent($"Preparing to Download {updateInfo.TargetFullRelease.Version}")
    //             .Dismiss()
    //             .After(2)
    //             .QueueAndWaitAsync();
    //
    //         _logger.LogInformation("Update Preparation Finished");
    //         var progressBar = new ProgressBar { Value = 0, ShowProgressText = true };
    //         var downloadToast = ToastManager
    //             .CreateToast()
    //             .WithTitle("Downloading Update")
    //             .WithContent(progressBar)
    //             .Queue();
    //         var progress = new Progress<Percentage>(percentage =>
    //             Dispatcher.UIThread.Invoke(() => progressBar.Value = percentage.Value)
    //         );
    //         await _updateService.PrepareUpdatesAsync(updateInfo, progress);
    //         ToastManager.Dismiss(downloadToast);
    //         ToastManager
    //             .CreateToast()
    //             .WithTitle("Download Finished")
    //             .WithContent($"Update {updateInfo.TargetFullRelease.Version} has been downloaded")
    //             .WithActionButtonNormal("Install Later", _ => { }, true)
    //             .WithActionButton(
    //                 "Install Now",
    //                 _ =>
    //                 {
    //                     _updateService.FinalizeUpdate();
    //                     if (Application.Current?.ApplicationLifetime?.TryShutdown(2) != true)
    //                         Environment.Exit(2);
    //                 }
    //             )
    //             .Queue();
    //
    //         OnPropertyChanged(new PropertyChangedEventArgs(Greeting));
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError(e, "Update Failed");
    //         ToastManager
    //             .CreateToast()
    //             .WithTitle("Error")
    //             .WithContent("An Error Occured While Performing the Update")
    //             .OfType(NotificationType.Error)
    //             .Dismiss()
    //             .ByClicking()
    //             .WithActionButton("Ok", true)
    //             .Queue();
    //     }
    // }
}
