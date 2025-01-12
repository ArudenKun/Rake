using System;
using Rake.ViewModels;
using SukiUI.Controls;

namespace Rake.Views;

public abstract class AbstractSukiWindow<TViewModel> : SukiWindow
    where TViewModel : AbstractViewModel
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
