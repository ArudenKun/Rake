using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gress;
using NuGet.Versioning;
using Rake.Core;
using Rake.Services;
using Rake.Utilities;
using SukiUI.Dialogs;

namespace Rake.ViewModels.Dialogs;

public sealed partial class UpdateViewModel : AbstractViewModel
{
    private readonly UpdateService _updateService;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private Percentage _downloadPercentage;

    [ObservableProperty]
    private string _downloadProgress = "0%";

    [ObservableProperty]
    private string _fileSize = "N/A";

    [ObservableProperty]
    private SemanticVersion _currentVersion = new(0, 0, 0);

    [ObservableProperty]
    private SemanticVersion _newVersion = new(0, 0, 0);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SkipCommand))]
    private bool _isUpdating;

    private ISukiDialog? _dialog;

    public ISukiDialog Dialog
    {
        get => _dialog ?? throw new InvalidOperationException("Dialog not set");
        set => _dialog = value;
    }

    public UpdateViewModel(UpdateService updateService, SettingsService settingsService)
    {
        _updateService = updateService;
        _settingsService = settingsService;

        CurrentVersion = _updateService.CurrentVersion;
        NewVersion = _updateService.NewVersion;
    }

    [RelayCommand]
    private async Task Update()
    {
        if (_updateService.UpdatePackage is null)
        {
            return;
        }

        IsUpdating = true;
        var progress = new BufferedProgress<Percentage>(percentage =>
        {
            DownloadPercentage = percentage;
            DownloadProgress = $"{percentage.Value}%";
        });
        await _updateService.PrepareUpdatesAsync(progress).ConfigureAwait(false);
        await progress.WaitToFinishAsync().ConfigureAwait(false);
        _updateService.FinalizeUpdate();
        ExitApplication();
    }

    private bool CanSkip => !IsUpdating;

    [RelayCommand(CanExecute = nameof(CanSkip))]
    private void Skip()
    {
        _settingsService.VersionsToSkip.Add(NewVersion);
        Dialog.Dismiss();
    }
}
