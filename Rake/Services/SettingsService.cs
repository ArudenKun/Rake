using System.Text.Json.Serialization;
using Cogwheel;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Rake.Helpers;
using Rake.Models;

namespace Rake.Services;

[INotifyPropertyChanged]
[PublicAPI]
public sealed partial class SettingsService()
    : SettingsBase(PathHelper.SettingsPath, SerializerContext.Default)
{
    private bool _isAutoUpdateEnabled = true;
    private int _parallelLimit = 4;
    private UpdateChannel _updateChannel = UpdateChannel.Stable;

    public bool IsAutoUpdateEnabled
    {
        get => _isAutoUpdateEnabled;
        set => SetProperty(ref _isAutoUpdateEnabled, value);
    }

    public int ParallelLimit
    {
        get => _parallelLimit;
        set => SetProperty(ref _parallelLimit, value);
    }

    public UpdateChannel UpdateChannel
    {
        get => _updateChannel;
        set => SetProperty(ref _updateChannel, value);
    }

    [JsonSerializable(typeof(SettingsService))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        UseStringEnumConverter = true,
        WriteIndented = true
    )]
    private partial class SerializerContext : JsonSerializerContext;
}
