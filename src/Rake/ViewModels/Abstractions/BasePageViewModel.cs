using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace Rake.ViewModels.Abstractions;

public abstract partial class BasePageViewModel : BaseViewModel, IPageViewModel
{
    [ObservableProperty]
    private bool _isPageActive;
    public virtual int PageIndex => 1;
    public virtual string PageName => GetType().Name.Replace("PageViewModel", string.Empty);
    public virtual MaterialIconKind PageIconKind => MaterialIconKind.Home;
}
