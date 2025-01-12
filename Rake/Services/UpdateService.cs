using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gress;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Velopack;

namespace Rake.Services;

public sealed class UpdateService
{
    private static readonly SemanticVersion EmptyVersion = new(0, 0, 0);
    private const string RepositoryUrl = "https://github.com/ArudenKun/Rake";

    private readonly ILogger<UpdateService> _logger;
    private readonly SettingsService _settingsService;

    private readonly Dictionary<string, UpdateManager> _updateManagers = new();
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

            _logger.LogDebug($"Current channel {channel}");

            if (_updateManagers.TryGetValue(channel, out var updateManager))
                return updateManager;

            updateManager = new UpdateManager(
                @"O:\Projects\Rake\releases",
                // new GithubSource(
                //     @"O:\Projects\Rake\releases",
                //     null,
                //     _settingsService.UpdateChannel
                //         is UpdateChannel.Beta
                //             or UpdateChannel.Dev
                //             or UpdateChannel.Alpha
                // ),
                new UpdateOptions { ExplicitChannel = channel },
                _logger
            );

            _updateManagers[channel] = updateManager;
            return updateManager;
        }
    }

    public SemanticVersion CurrentVersion => UpdateManager.CurrentVersion ?? EmptyVersion;

    public UpdateInfo? UpdatePackage { get; private set; }

    public async Task<UpdateInfo?> CheckForUpdatesAsync(bool ignoreCache = false)
    {
        if (!UpdateManager.IsInstalled)
            return null;

        if (UpdatePackage is not null && !ignoreCache)
        {
            return UpdatePackage;
        }

        return UpdatePackage = await UpdateManager.CheckForUpdatesAsync();
    }

    public async Task PrepareUpdatesAsync(IProgress<Percentage>? progress = null)
    {
        if (!UpdateManager.IsInstalled || UpdatePackage is null)
            return;

        try
        {
            await UpdateManager.DownloadUpdatesAsync(
                UpdatePackage,
                i => progress?.Report(Percentage.FromValue(i))
            );
            _updatePrepared = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to prepare updates");
        }
    }

    public void FinalizeUpdate(
        bool restart = true,
        bool silent = false,
        params string[] restartArgs
    )
    {
        if (
            !UpdateManager.IsInstalled
            || UpdatePackage is null
            || !_updatePrepared
            || _updaterLaunched
        )
            return;

        UpdateManager.WaitExitThenApplyUpdates(UpdatePackage, silent, restart, restartArgs);
        _updaterLaunched = true;
    }
}
