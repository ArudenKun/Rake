using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Rake.Generators;
using Wpf.Ui.Controls;

namespace Rake.ViewModels.Abstractions;

[ObservableRecipient]
public abstract partial class BaseViewModel : ObservableValidator, INavigationAware, IActivatable
{
    public void Activate()
    {
        Loaded();
    }

    public void Deactivate()
    {
        UnLoaded();
    }

    protected virtual void Loaded() { }

    protected virtual void UnLoaded() { }

    /// <inheritdoc cref="OnNavigatedTo"/>
    public virtual async Task OnNavigatedToAsync()
    {
        using CancellationTokenSource cts = new();

        await DispatchAsync(OnNavigatedTo, cts.Token);
    }

    /// <inheritdoc />
    public virtual void OnNavigatedTo() { }

    /// <inheritdoc cref="OnNavigatedFrom"/>
    public virtual async Task OnNavigatedFromAsync()
    {
        using CancellationTokenSource cts = new();

        await DispatchAsync(OnNavigatedFrom, cts.Token);
    }

    /// <inheritdoc />
    public virtual void OnNavigatedFrom() { }

    /// <summary>
    /// Dispatches the specified action on the UI thread.
    /// </summary>
    /// <param name="action">The action to be dispatched.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected static async Task DispatchAsync(Action action, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(action);
    }
}
