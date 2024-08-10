using System.ComponentModel;

namespace Rake.Generators.Interfaces;

public interface IView<TViewModel>
    where TViewModel : INotifyPropertyChanged
{
    TViewModel ViewModel { get; set; }
}
