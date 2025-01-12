using System;
using Avalonia.Controls;
using Rake.ViewModels;

namespace Rake.Views;

public abstract class AbstractUserControl<TViewModel> : UserControl
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
