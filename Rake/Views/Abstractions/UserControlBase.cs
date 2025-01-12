using System;
using Avalonia.Controls;
using ViewModelBase = Rake.ViewModels.Abstractions.ViewModelBase;

namespace Rake.Views.Abstractions;

public abstract class UserControlBase<TViewModel> : UserControl
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
