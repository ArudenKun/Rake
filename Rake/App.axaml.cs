using System;
using AsyncNavigation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using R3;
using R3.ObservableEvents;
using Rake.Core;
using Rake.ViewModels;
using Rake.Views;
using Volo.Abp.DependencyInjection;

namespace Rake;

[PublicAPI]
public class App : Application, IDisposable
{
    private const string MutexId = "EB84BD0E-A9BC-4937-9C8D-395E0890FB79";
    private ResourceMutex? _resourceMutex;
    private IpcBroker? _ipcBroker;
    private WindowState _lastPreMinimizedState;
    private CompositeDisposable _disposables = [];

    public required IAbpLazyServiceProvider LazyServiceProvider { get; init; }

    protected ILoggerFactory LoggerFactory =>
        LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        LazyServiceProvider.LazyGetService<ILogger>(_ =>
            LoggerFactory.CreateLogger(GetType().FullName!)
        );

    // protected IThemeService ThemeService => LazyServiceProvider.GetRequiredService<IThemeService>();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        GlobalExceptionHandler.Install(LoggerFactory.CreateLogger("GlobalExceptionHandler"));

        _resourceMutex = ResourceMutex.Create(
            LoggerFactory.CreateLogger<ResourceMutex>(),
            MutexId,
            RakeConsts.Name
        );
        _ipcBroker = new IpcBroker(
            LoggerFactory.CreateLogger<IpcBroker>(),
            $"{RakeConsts.Name}-{MutexId}"
        );

        if (!_resourceMutex.IsLocked)
        {
            Logger.LogInformation("Another instance is already running");
            _ipcBroker.SignalRunningInstanceAsync().ContinueWith(_ => desktop.TryShutdown());
            return;
        }

        // ThemeService.Initialize();

        var mainWindow = LazyServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = LazyServiceProvider.GetRequiredService<MainWindowViewModel>();
        desktop.MainWindow = mainWindow;

        mainWindow
            .GetObservable(Window.WindowStateProperty)
            .ToObservable()
            .Subscribe(state =>
            {
                if (state is not WindowState.Minimized)
                    _lastPreMinimizedState = state;
            })
            .AddTo(_disposables);

        _ipcBroker
            .Events()
            .OnFocusRequested.Subscribe(_ =>
                Dispatcher.Post(() =>
                {
                    var window = desktop.MainWindow;
                    if (window.WindowState == WindowState.Minimized)
                        window.WindowState = _lastPreMinimizedState;

                    window.Show();
                    window.Activate();
                    window.Focus();
                    window.Topmost = true;
                    window.Topmost = false;
                })
            )
            .AddTo(_disposables);

        _ipcBroker.Start();

        base.OnFrameworkInitializationCompleted();
    }

    public void Dispose()
    {
        _resourceMutex?.Dispose();
        _ipcBroker?.Dispose();
        _disposables.Dispose();
    }
}
