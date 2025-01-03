using System;
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
    private readonly ILogger<UpdateService> _logger;
    private readonly SettingsService _settingsService;
    private const string RepositoryUrl = "https://github.com/ArudenKun/Rake";

    public UpdateService(ILogger<UpdateService> logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    public bool UpdateReady => Manager.UpdatePendingRestart is not null;

    private UpdateManager Manager =>
        new(
            new GithubSource(
                RepositoryUrl,
                null,
                _settingsService.UpdateChannel is UpdateChannel.Beta or UpdateChannel.Dev
            ),
            new UpdateOptions
            {
                ExplicitChannel =
                    $"{_settingsService.UpdateChannel}.{RuntimeInformation.RuntimeIdentifier}",
            },
            _logger
        );

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        if (!_settingsService.IsAutoUpdateEnabled)
        {
            return null;
        }

        return await Manager.CheckForUpdatesAsync().ConfigureAwait(false);
    }

    public async Task PrepareUpdatesAsync(
        UpdateInfo updateInfo,
        IProgress<Percentage>? progress = null
    ) =>
        await Manager
            .DownloadUpdatesAsync(updateInfo, i => progress?.Report(Percentage.FromValue(i)))
            .ConfigureAwait(false);

    public void ApplyUpdates(UpdateInfo updateInfo, bool restart = true)
    {
        if (restart)
        {
            Manager.ApplyUpdatesAndRestart(updateInfo);
        }
        else
        {
            Manager.ApplyUpdatesAndExit(updateInfo);
        }
    }
}
