using System.ComponentModel;

namespace Rake.Generator.Interfaces;

public interface IView<TViewModel>
    where TViewModel : INotifyPropertyChanged
{
    TViewModel ViewModel { get; init; }
}
