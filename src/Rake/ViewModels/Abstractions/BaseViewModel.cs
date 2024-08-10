using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rake.ViewModels.Abstractions;

[ObservableRecipient]
public abstract partial class BaseViewModel : ObservableValidator, IViewModel
{
    protected CompositeDisposable Disposables { get; } = new();

    public void Loaded()
    {
        OnLoaded();
    }

    public void Unloaded()
    {
        OnUnloaded();
    }

    public void AttachedToVisualTree()
    {
        OnAttachedToVisualTree();
    }

    public void DetachedFromVisualTree()
    {
        OnDetachedFromVisualTree();
    }

    protected virtual void OnLoaded() { }

    protected virtual void OnUnloaded() { }

    protected virtual void OnAttachedToVisualTree() { }

    protected virtual void OnDetachedFromVisualTree() { }

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

        await Dispatcher.UIThread.InvokeAsync(action);
    }

    ~BaseViewModel()
    {
        Dispose(false);
    }

    protected void OnAllPropertiesChanged()
    {
        OnPropertyChanged(string.Empty);
    }

    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        Dispose(true);
        Disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
