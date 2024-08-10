using Wpf.Ui.Controls;

namespace Rake.ViewModels.Abstractions;

public abstract class BasePageViewModel : BaseViewModel
{
    public virtual string Name => GetType().Name.Replace("PageViewModel", string.Empty);
    public virtual int Index { get; } = 0;
    public virtual bool IsFooter { get; } = false;
    public virtual SymbolRegular Icon { get; } = SymbolRegular.Home24;
    public abstract Type ViewType { get; }
}
