using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gress;
using Microsoft.Extensions.Logging;
using Rake.Models;
using Velopack;
using Velopack.Sources;

namespace Rake.Services;

public sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/ArudenKun/Rake";

    private readonly ILogger<UpdateService> _logger;
    private readonly SettingsService _settingsService;

    private readonly Dictionary<string, UpdateManager> _updateManagers = new();
    private UpdateInfo? _updateInfo;
    private bool _updatePrepared;
    private bool _updaterLaunched;

    public UpdateService(ILogger<UpdateService> logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    private UpdateManager UpdateManager
    {
        get
        {
            var channel =
                $"{_settingsService.UpdateChannel}.{RuntimeInformation.RuntimeIdentifier}";

            if (_updateManagers.TryGetValue(channel, out var updateManager))
                return updateManager;

            updateManager = new UpdateManager(
                new GithubSource(
                    RepositoryUrl,
                    null,
                    _settingsService.UpdateChannel is UpdateChannel.Beta or UpdateChannel.Dev
                ),
                new UpdateOptions { ExplicitChannel = channel },
                _logger
            );

            _updateManagers[channel] = updateManager;

            return updateManager;
        }
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        if (!UpdateManager.IsInstalled || !_settingsService.IsAutoUpdateEnabled)
            return null;

        return await UpdateManager.CheckForUpdatesAsync();
    }

    public async Task PrepareUpdatesAsync(
        UpdateInfo updateInfo,
        IProgress<Percentage>? progress = null
    )
    {
        if (!UpdateManager.IsInstalled || !_settingsService.IsAutoUpdateEnabled)
            return;

        try
        {
            await UpdateManager.DownloadUpdatesAsync(
                _updateInfo = updateInfo,
                i => progress?.Report(Percentage.FromValue(i))
            );
            _updatePrepared = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to prepare updates");
        }
    }

    public void FinalizeUpdate(bool restart = true)
    {
        if (!UpdateManager.IsInstalled || !_settingsService.IsAutoUpdateEnabled)
            return;

        if (_updateInfo is null || !_updatePrepared || _updaterLaunched)
            return;

        UpdateManager.WaitExitThenApplyUpdates(_updateInfo, false, restart);
        _updaterLaunched = true;
    }
}
