using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using R3;
using Rake.Extensions;
using Rake.Services;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Rake.ViewModels;

public sealed partial class MainWindowViewModel : AbstractViewModel
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly UpdateService _updateService;
    private readonly SettingsService _settingsService;
    private readonly ViewModelFactory _viewModelFactory;

    [ObservableProperty]
    private string _url = "https://www.youtube.com";

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        UpdateService updateService,
        SettingsService settingsService,
        ViewModelFactory viewModelFactory
    )
    {
        _logger = logger;
        _updateService = updateService;
        _settingsService = settingsService;
        _viewModelFactory = viewModelFactory;

        Observable
            .EveryValueChanged(this, x => x.Url)
            .Debounce(1.Seconds())
            .Subscribe(s =>
            {
                logger.LogInformation(s);
            })
            .AddTo(this);
    }

    protected override async Task OnLoadedAsync()
    {
        await CheckForUpdatesAsync();
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

    private async Task CheckForUpdatesAsync()
    {
        if (!_settingsService.IsAutoCheckForUpdatesEnabled || !_updateService.IsInstalled)
        {
            return;
        }

        await Task.Delay(1.Seconds());
        _logger.LogInformation("Checking for updates");
        await ToastManager
            .CreateSimpleInfoToast()
            .WithTitle("Update")
            .WithContent("Checking for updates")
            .QueueAsync();

        var updateInfo = await _updateService.CheckForUpdatesAsync(true);
        if (updateInfo is null)
        {
            await Task.Delay(500);
            _logger.LogInformation("No updates found");
            ToastManager
                .CreateSimpleInfoToast()
                .WithTitle("Update")
                .WithContent("No updates found")
                .Queue();
            return;
        }

        if (
            _settingsService.VersionsToSkip.TryGetValue(
                updateInfo.TargetFullRelease.Version,
                out var skippedVersion
            )
        )
        {
            _logger.LogInformation("Skipping update {0}", updateInfo.TargetFullRelease.Version);
            ToastManager
                .CreateSimpleInfoToast()
                .WithTitle("Skipping Update")
                .WithContent($"Skipping {skippedVersion}")
                .Queue();
            return;
        }

        _logger.LogInformation("Updates found {0}", updateInfo.TargetFullRelease.Version);
        ShowUpdateDialog();
    }

    [RelayCommand]
    private void ShowUpdateDialog()
    {
        DialogManager
            .CreateDialog()
            .WithViewModel(_viewModelFactory.CreateUpdateViewModel)
            .TryShow();
    }
}
