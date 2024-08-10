using System;
using AutoInterfaceAttributes;
using Microsoft.Extensions.DependencyInjection;
using Rake.ViewModels.Abstractions;

namespace Rake.Services;

[AutoInterface]
public sealed class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TViewModel CreateViewModel<TViewModel>()
        where TViewModel : IViewModel => _serviceProvider.GetRequiredService<TViewModel>();

    public object CreateViewModel(Type viewModelType) =>
        _serviceProvider.GetRequiredService(viewModelType);
}
