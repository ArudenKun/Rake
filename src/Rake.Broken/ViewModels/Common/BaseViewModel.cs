using System;
using System.ComponentModel;
using AutoInterfaceAttributes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rake.ViewModels.Common;

[AutoInterface(
    Name = "IViewModel",
    Inheritance = [
        typeof(IDisposable),
        typeof(INotifyPropertyChanged),
        typeof(INotifyPropertyChanging),
        typeof(INotifyDataErrorInfo),
        typeof(IViewModelEvents)
    ]
)]
[ObservableRecipient]
public abstract partial class BaseViewModel : ObservableValidator, IViewModel
{
    #region View Events

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

    #endregion

    #region Dispose

    ~BaseViewModel() => Dispose(false);

    protected void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
