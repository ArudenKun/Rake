using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AutoInterfaceAttributes;
using Gress;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Rake.Services;

public sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/ArudenKun/Rake";

    private readonly ILogger<UpdateService> _logger;
    private readonly SettingsService _settingsService;
    private readonly UpdateManager _updateManager;

    private UpdateInfo? _updateInfo;
    private bool _updatePrepared;
    private bool _updaterLaunched;

    public UpdateService(ILogger<UpdateService> logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        // _updateManager = new UpdateManager(
        //     new GithubSource(RepositoryUrl, null, true),
        //     new UpdateOptions
        //     {
        //         ExplicitChannel =
        //             $"{_settingsService.UpdateChannel}.{RuntimeInformation.RuntimeIdentifier}",
        //     },
        //     _logger
        // );
        _updateManager = new UpdateManager(@"O:\Projects\Rake\releases", null, logger);
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        if (!_updateManager.IsInstalled || !_settingsService.IsAutoUpdateEnabled)
            return null;

        return await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
    }

    public async Task PrepareUpdatesAsync(
        UpdateInfo updateInfo,
        IProgress<Percentage>? progress = null
    )
    {
        if (!_updateManager.IsInstalled || !_settingsService.IsAutoUpdateEnabled)
            return;

        try
        {
            await _updateManager
                .DownloadUpdatesAsync(
                    _updateInfo = updateInfo,
                    i => progress?.Report(Percentage.FromValue(i))
                )
                .ConfigureAwait(false);
            _updatePrepared = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to prepare updates");
        }
    }

    public void FinalizeUpdate(bool restart = true)
    {
        if (!_updateManager.IsInstalled || !_settingsService.IsAutoUpdateEnabled)
            return;

        if (_updateInfo is null || !_updatePrepared || _updaterLaunched)
            return;

        _updateManager.WaitExitThenApplyUpdates(_updateInfo, restart);
        _updaterLaunched = true;
    }
}
