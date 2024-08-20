using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoInterfaceAttributes;
using Avalonia.Styling;
using Cogwheel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Rake.Core.Helpers;
using Rake.Models;

namespace Rake.Services;

[INotifyPropertyChanged]
[AutoInterface(Inheritance = [typeof(IDisposable), typeof(INotifyPropertyChanged)])]
public sealed partial class SettingsService : SettingsBase, ISettingsService
{
    private readonly ILogger<SettingsService> _logger;

    private Theme _theme;

    public SettingsService(
        ILogger<SettingsService> logger,
        JsonSerializerOptions jsonSerializerOptions
    )
        : base(EnvironmentHelper.AppDataDirectory.JoinPath("settings.json"), jsonSerializerOptions)
    {
        _logger = logger;
    }

    public Theme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    [JsonIgnore]
    public ThemeVariant ThemeVariant =>
        Theme switch
        {
            Theme.Light => ThemeVariant.Light,
            Theme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

    public void Dispose()
    {
        Save();
    }
}
