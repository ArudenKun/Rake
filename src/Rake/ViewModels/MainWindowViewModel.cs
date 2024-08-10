using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Rake.Generators.Attributes;
using Rake.ViewModels.Abstractions;

namespace Rake.ViewModels;

[Singleton]
public partial class MainWindowViewModel : BaseViewModel
{
    [ObservableProperty]
    private IPageViewModel _activePage = null!;

    public IEnumerable<IPageViewModel> Pages { get; }

    public MainWindowViewModel(IEnumerable<BasePageViewModel> pageViewModels)
    {
        Pages = pageViewModels.OrderBy(x => x.PageIndex);
    }
}
