using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rake.ViewModels.Abstractions;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    public virtual void Activate() { }

    public virtual void Deactivate() { }

    ~ViewModelBase() => Dispose(false);

    protected void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
