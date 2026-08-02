using AsyncNavigation.Abstractions;
using Rake.ViewModels;

namespace Rake.Views;

public interface IView<TViewModel> : IView
    where TViewModel : ViewModel
{
    new TViewModel DataContext { get; set; }
}
