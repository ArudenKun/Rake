using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using AsyncImageLoader;
using AutoInterfaceAttributes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Rake.Core.Extensions;
using Rake.Core.Utilities;
using Rake.Services;
using Rake.Services.Caching;
using Rake.ViewModels;

namespace Rake;

[SuppressMessage(
    "Trimming",
    "IL2046:\'RequiresUnreferencedCodeAttribute\' annotations must match across all interface implementations or overrides."
)]
[AutoInterface(
    Inheritance = [
        typeof(IDisposable),
        typeof(INotifyPropertyChanged),
        typeof(IDataContextProvider),
        typeof(IGlobalDataTemplates),
        typeof(IDataTemplateHost),
        typeof(IGlobalStyles),
        typeof(IStyleHost),
        typeof(IThemeVariantHost),
        typeof(IResourceHost),
        typeof(IResourceNode),
        typeof(IApplicationPlatformEvents),
        typeof(IOptionalFeatureProvider)
    ]
)]
public sealed class App : Application, IApp
{
    private readonly DisposableCollection _disposableCollection = new();

    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly ISettingsService _settingsService;

    public App(
        MainWindowViewModel mainWindowViewModel,
        ISettingsService settingsService,
        FileCacheImageLoader fileCacheImageLoader
    )
    {
        _mainWindowViewModel = mainWindowViewModel;
        _settingsService = settingsService;

        ImageLoader.AsyncImageLoader = fileCacheImageLoader;
        ImageBrushLoader.AsyncImageLoader = fileCacheImageLoader;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    [RequiresUnreferencedCode("BindingPlugins.DataValidators uses reflection")]
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = DataTemplates[0].Build(_mainWindowViewModel) as Window;
        }

        base.OnFrameworkInitializationCompleted();

        _disposableCollection.Add(
            _settingsService.WatchProperty(
                x => x.Theme,
                () => RequestedThemeVariant = _settingsService.ThemeVariant
            )
        );
    }

    public void Dispose()
    {
        _disposableCollection.Dispose();
    }
}
