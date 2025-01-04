using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Rake.ViewModels.Abstractions;

public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    public ISukiDialogManager DialogManager { get; } = new SukiDialogManager();
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();

    [RelayCommand]
    private Task Loaded() => OnLoadedAsync();

    protected virtual Task OnLoadedAsync() => Task.CompletedTask;

    ~ViewModelBase() => Dispose(false);

    protected void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    /// <inheritdoc cref="Dispose"/>>
    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
