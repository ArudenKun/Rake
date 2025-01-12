using System;
using System.ComponentModel;
using System.Threading.Tasks;
using AutoInterfaceAttributes;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rake.Extensions;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Rake.ViewModels.Abstractions;

[AutoInterface(
    Name = "IViewModel",
    Inheritance = [
        typeof(IDisposable),
        typeof(INotifyPropertyChanged),
        typeof(INotifyPropertyChanging),
    ]
)]
public abstract partial class ViewModelBase : ObservableRecipient, IViewModel
{
    public ISukiDialogManager DialogManager { get; } = new SukiDialogManager();
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();

    [RelayCommand]
    private Task Loaded() => OnLoadedAsync();

    protected virtual Task OnLoadedAsync() => Task.CompletedTask;

    protected static void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime?.TryShutdown(2) is not true)
            Environment.Exit(2);
    }

    /// <summary>
    /// Dispatches the specified action on the UI thread synchronously.
    /// </summary>
    /// <param name="action">The action to be dispatched.</param>
    protected static void Dispatch(Action action) => Dispatcher.UIThread.Invoke(action);

    /// <summary>
    /// Dispatches the specified action on the UI thread synchronously.
    /// </summary>
    /// <param name="action">The action to be dispatched.</param>
    protected static TResult Dispatch<TResult>(Func<TResult> action) =>
        Dispatcher.UIThread.Invoke(action);

    /// <summary>
    /// Dispatches the specified action on the UI thread.
    /// </summary>
    /// <param name="action">The action to be dispatched.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected static async Task DispatchAsync(Action action) =>
        await Dispatcher.UIThread.InvokeAsync(action);

    /// <summary>
    /// Dispatches the specified action on the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to be dispatched.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected static async Task<TResult> DispatchAsync<TResult>(Func<TResult> action) =>
        await Dispatcher.UIThread.InvokeAsync(action);

    ~ViewModelBase() => Dispose(false);

    protected void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    /// <inheritdoc cref="Dispose"/>>
    protected virtual void Dispose(bool disposing) { }

    /// <inheritdoc />>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
