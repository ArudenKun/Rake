using System;
using SukiUI.Controls;
using ViewModelBase = Rake.ViewModels.Abstractions.ViewModelBase;

namespace Rake.Views.Abstractions;

public abstract class SukiWindowBase<TViewModel> : SukiWindow
    where TViewModel : ViewModelBase
{
    public new TViewModel DataContext
    {
        get =>
            base.DataContext as TViewModel
            ?? throw new InvalidCastException(
                $"DataContext is null or not of the expected type '{typeof(TViewModel).FullName}'."
            );
        set => base.DataContext = value;
    }
}
