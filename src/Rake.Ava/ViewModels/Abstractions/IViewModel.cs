using System;
using System.ComponentModel;

namespace Rake.ViewModels.Abstractions;

public interface IViewModel
    : IDisposable,
        INotifyPropertyChanged,
        INotifyPropertyChanging,
        INotifyDataErrorInfo,
        IViewModelEvents;
