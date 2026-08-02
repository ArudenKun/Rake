using System;
using Avalonia.Controls;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Rake.ViewModels;
using Volo.Abp.DependencyInjection;

namespace Rake.Views;

public abstract class Window<TViewModel> : Window, IView<TViewModel>
    where TViewModel : ViewModel
{
    [PublicAPI]
    public required IAbpLazyServiceProvider LazyServiceProvider { protected get; init; }

    protected ILoggerFactory LoggerFactory =>
        LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        LazyServiceProvider.LazyGetService<ILogger>(_ =>
            LoggerFactory.CreateLogger(GetType().FullName!)
        );

    public new TViewModel DataContext
    {
        get =>
            base.DataContext as TViewModel
            ?? throw new InvalidCastException(
                $"DataContext is null or not of the expected type '{typeof(TViewModel).FullName}'."
            );
        set => base.DataContext = value;
    }

    public TViewModel ViewModel => DataContext;
}
