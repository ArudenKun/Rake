using System;
using AutoInterfaceAttributes;
using Avalonia.Controls;
using Rake.ViewModels.Abstractions;

namespace Rake.Services;

[AutoInterface]
public sealed class ViewFactory : IViewFactory
{
    private readonly IViewModelFactory _viewModelFactory;

    public ViewFactory(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public Control CreateControl<TViewModel>()
        where TViewModel : IViewModel => CreateControl(typeof(TViewModel));

    public Control CreateControl(Type type)
    {
        if (!ViewLocator.ViewMap.TryGetValue(type, out var control))
        {
            throw new TypeLoadException($"No view registered for {type.FullName}");
        }

        return control(_viewModelFactory.CreateViewModel(type));
    }
}
