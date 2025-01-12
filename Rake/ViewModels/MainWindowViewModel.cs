using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Humanizer;
using Microsoft.Extensions.Logging;
using Rake.Extensions;
using Rake.Models.Messages;
using Rake.Services;
using Rake.ViewModels.Abstractions;
using SukiUI.Toasts;

namespace Rake.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IRecipient<UpdateSkippedMessage>
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ViewModelFactory _viewModelFactory;
    private readonly UpdateService _updateService;
    private readonly SettingsService _settingsService;
    private readonly DashboardViewModel _dashboardViewModel;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ViewModelFactory viewModelFactory,
        UpdateService updateService,
        SettingsService settingsService,
        DashboardViewModel dashboardViewModel
    )
    {
        Messenger.Register(this);

        _logger = logger;
        _viewModelFactory = viewModelFactory;
        _updateService = updateService;
        _settingsService = settingsService;
        _dashboardViewModel = dashboardViewModel;

        // CurrentViewModel = _serviceProvider.GetRequiredService<UpdateViewModel>();
        CurrentViewModel = _dashboardViewModel;
    }

    protected override async Task OnLoadedAsync()
    {
        _settingsService.Load();
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
        if (!_settingsService.IsAutoCheckForUpdatesEnabled)
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

        if (_settingsService.VersionsToSkip.Contains(updateInfo.TargetFullRelease.Version))
        {
            _logger.LogInformation("Skipping update {0}", updateInfo.TargetFullRelease.Version);
            ToastManager
                .CreateSimpleInfoToast()
                .WithTitle("Skipping Update")
                .WithContent("")
                .Queue();
            return;
        }

        _logger.LogInformation("Updates found {0}", updateInfo.TargetFullRelease.Version);
        CurrentViewModel = _viewModelFactory.CreateUpdateViewModel(
            updateInfo,
            _updateService.CurrentVersion
        );
    }

    public void Receive(UpdateSkippedMessage message)
    {
        _settingsService.VersionsToSkip.Add(message.Version);
        _logger.LogInformation("Update skipped, Loading dashboard");
        CurrentViewModel = _dashboardViewModel;
    }
}
