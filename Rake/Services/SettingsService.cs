using System.Collections.Generic;
using Cogwheel;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Rake.Core.Helpers;
using Rake.Models;

namespace Rake.Services;

[INotifyPropertyChanged]
[PublicAPI]
public sealed partial class SettingsService : SettingsBase
{
    private readonly ILogger<SettingsService> _logger;

    private bool _isAutoCheckForUpdatesEnabled = true;
    private UpdateChannel _updateChannel = UpdateChannel.Stable;
    private HashSet<SemanticVersion> _versionsToSkip = [];
    private bool _isMultipleInstancesEnabled;
    private int _parallelLimit = 4;

    /// <inheritdoc/>
    public SettingsService(ILogger<SettingsService> logger)
        : base(PathHelper.SettingsPath, GlobalJsonSerializerContext.Default.Options)
    {
        _logger = logger;
    }

    public bool IsAutoCheckForUpdatesEnabled
    {
        get => _isAutoCheckForUpdatesEnabled;
        set => SetProperty(ref _isAutoCheckForUpdatesEnabled, value);
    }

    public UpdateChannel UpdateChannel
    {
        get => _updateChannel;
        set => SetProperty(ref _updateChannel, value);
    }

    public HashSet<SemanticVersion> VersionsToSkip
    {
        get => _versionsToSkip;
        set => SetProperty(ref _versionsToSkip, value);
    }

    public bool IsMultipleInstancesEnabled
    {
        get => _isMultipleInstancesEnabled;
        set => SetProperty(ref _isMultipleInstancesEnabled, value);
    }

    public int ParallelLimit
    {
        get => _parallelLimit;
        set => SetProperty(ref _parallelLimit, value);
    }

    public ThemeVariant Theme { get; set; } = ThemeVariant.Default;
}
