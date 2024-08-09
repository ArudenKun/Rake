using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Rake.Core.Extensions;
using Rake.Services;
using Rake.Services.Caching;
using Rake.ViewModels;

namespace Rake;

public sealed class App : Application, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    private readonly ISettingsService _settingsService;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly FileCacheImageLoader _fileCacheImageLoader;

    public App(
        ISettingsService settingsService,
        MainWindowViewModel mainWindowViewModel,
        FileCacheImageLoader fileCacheImageLoader
    )
    {
        _settingsService = settingsService;
        _mainWindowViewModel = mainWindowViewModel;
        _fileCacheImageLoader = fileCacheImageLoader;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    [RequiresUnreferencedCode("BindingPlugins.DataValidators.RemoveAt(Int) requires reflection")]
#pragma warning disable IL2046
    public override void OnFrameworkInitializationCompleted()
#pragma warning restore IL2046
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            // desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
            desktop.MainWindow = DataTemplates[0].Build(_mainWindowViewModel) as Window;
        }

        base.OnFrameworkInitializationCompleted();

        _settingsService
            .WatchProperty(
                x => x.Theme,
                () => RequestedThemeVariant = _settingsService.ThemeVariant
            )
            .DisposeWith(_disposables);

        ImageLoader.AsyncImageLoader = _fileCacheImageLoader;
        ImageBrushLoader.AsyncImageLoader = _fileCacheImageLoader;
    }
}
