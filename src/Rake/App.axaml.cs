using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using AvaloniaWebView;
using Rake.Core.Extensions;
using Rake.Services;
using Rake.ViewModels;

namespace Rake;

public sealed class App : Application, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly ISettingsService _settingsService;

    public App(ISettingsService settingsService, MainWindowViewModel mainWindowViewModel)
    {
        _settingsService = settingsService;
        _mainWindowViewModel = mainWindowViewModel;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        AvaloniaWebViewBuilder.Initialize(config => config.IsInPrivateModeEnabled = true);
    }

    public override void OnFrameworkInitializationCompleted()
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
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
