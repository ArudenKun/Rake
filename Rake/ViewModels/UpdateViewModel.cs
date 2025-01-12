using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gress;
using NuGet.Versioning;
using Rake.Core;
using Rake.Models.Messages;
using Rake.Services;
using Rake.ViewModels.Abstractions;
using Velopack;

namespace Rake.ViewModels;

public sealed partial class UpdateViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;

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
    private VelopackAsset? _updatePackage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SkipCommand))]
    private bool _isUpdating;

    public UpdateViewModel(UpdateService updateService) => _updateService = updateService;

    [RelayCommand]
    private async Task Update()
    {
        if (UpdatePackage is null)
            return;

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
    private void Skip() => Messenger.Send(new UpdateSkippedMessage(NewVersion));
}
