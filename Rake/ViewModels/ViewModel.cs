using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncNavigation;
using AsyncNavigation.Abstractions;
using AsyncNavigation.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Rake.ViewModels;

[PublicAPI]
public abstract partial class ViewModel
    : ObservableObject,
        IUnitOfWorkEnabled,
        IHasExtraProperties,
        INavigationAware
{
    protected ViewModel()
    {
        ExtraProperties = new ExtraPropertyDictionary();
        this.SetDefaultsForExtraProperties();
    }

    public required IAbpLazyServiceProvider LazyServiceProvider { protected get; init; }

    protected JoinableTaskFactory JoinableTaskFactory =>
        LazyServiceProvider.LazyGetRequiredService<JoinableTaskFactory>();

    protected ILoggerFactory LoggerFactory =>
        LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        LazyServiceProvider.LazyGetService<ILogger>(_ =>
            LoggerFactory.CreateLogger(GetType().FullName!)
        );

    protected IClock Clock => LazyServiceProvider.LazyGetRequiredService<IClock>();

    protected IGuidGenerator GuidGenerator =>
        LazyServiceProvider.LazyGetRequiredService<IGuidGenerator>();

    protected ILocalEventBus LocalEventBus =>
        LazyServiceProvider.LazyGetRequiredService<ILocalEventBus>();

    public ExtraPropertyDictionary ExtraProperties { get; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string IsBusyText { get; set; } = string.Empty;

    protected virtual async Task SetBusyAsync(
        Func<Task> func,
        string busyText = "",
        bool showException = true
    )
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync();
        IsBusy = true;
        IsBusyText = busyText;
        try
        {
            await func();
        }
        catch (Exception ex) when (LogException(ex, true, showException))
        {
            // Not Used
        }
        finally
        {
            IsBusy = false;
            IsBusyText = string.Empty;
        }
    }

    protected bool LogException(Exception? ex, bool shouldCatch = false, bool shouldDisplay = false)
    {
        if (ex is null)
            return shouldCatch;

        Logger.LogException(ex);
        if (shouldDisplay)
            GlobalExceptionHandler.Show(ex);

        return shouldCatch;
    }

    protected virtual void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    #region Navigation

    protected IRegionManager RegionManager =>
        LazyServiceProvider.LazyGetRequiredService<IRegionManager>();

    protected void Navigate<TView>(string region)
        where TView : class, IView =>
        RegionManager
            .RequestNavigateAsync<TView>(region)
            .SafeFireAndForget(ex => Logger.LogException(ex));

    protected void Navigate(Type viewType, string region) =>
        RegionManager
            .RequestNavigateAsync(viewType, region)
            .SafeFireAndForget(ex => Logger.LogException(ex));

    /// <inheritdoc/>
    /// <remarks>Called only the first time a view is created and shown. Default implementation does nothing.</remarks>
    public virtual Task InitializeAsync(NavigationContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>Called every time the view becomes the active view. Default implementation does nothing.</remarks>
    public virtual Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>Called when navigating away from this view. Default implementation does nothing.</remarks>
    public virtual Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>
    /// Controls whether a cached view instance can be reused for the incoming navigation request.
    /// Default returns <see langword="true"/>, meaning the cached instance is always reused.
    /// Override and return <see langword="false"/> to force creation of a new instance.
    /// </remarks>
    public virtual Task<bool> IsNavigationTargetAsync(NavigationContext context) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    /// <remarks>Called when the view is being removed from the region cache. Default implementation does nothing.</remarks>
    public virtual Task OnUnloadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>
    /// Raise this event to request that the framework proactively removes this view from the region.
    /// Use <see cref="RequestUnloadAsync"/> as a convenient helper to raise it.
    /// </remarks>
    public event AsyncNavigation.Core.AsyncEventHandler<AsyncEventArgs>? AsyncRequestUnloadEvent;

    /// <summary>
    /// Raises <see cref="AsyncRequestUnloadEvent"/> to request that the framework remove this view.
    /// </summary>
    protected Task RequestUnloadAsync(CancellationToken cancellationToken = default)
    {
        var handler = AsyncRequestUnloadEvent;
        return handler is not null
            ? handler(this, new AsyncEventArgs(cancellationToken))
            : Task.CompletedTask;
    }

    #endregion
}
