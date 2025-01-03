using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rake.Extensions;
using Rake.Services;
using Rake.ViewModels;
using Serilog;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Rake;

public sealed class App : Application, IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();

        services.AddSingleton<SettingsService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();
        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddViewModels();
        services.AddLogging(builder => builder.ClearProviders().AddSerilog(dispose: true));
        _serviceProvider = services.BuildServiceProvider(true);
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    [RequiresUnreferencedCode("Calls Avalonia.Data.Core.Plugins.BindingPlugins.DataValidators")]
#pragma warning disable IL2046
    public override void OnFrameworkInitializationCompleted()
#pragma warning restore IL2046
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            logger.LogInformation("Application starting");
            desktop.Exit += (_, _) =>
            {
                logger.LogInformation("Application stopped");
            };
            desktop.MainWindow = (Window?)
                DataTemplates
                    .First()
                    .Build(_serviceProvider.GetRequiredService<MainWindowViewModel>());
        }

        base.OnFrameworkInitializationCompleted();
    }

    [RequiresUnreferencedCode("Calls Avalonia.Data.Core.Plugins.BindingPlugins.DataValidators")]
    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
