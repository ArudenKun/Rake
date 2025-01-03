using System;
using JetBrains.Annotations;
using Rake.ViewModels.Abstractions;
using SukiUI.Controls;

namespace Rake.Views.Abstractions;

[PublicAPI]
public abstract class SukiWindow<TViewModel> : SukiWindow
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
