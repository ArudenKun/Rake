using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using JetBrains.Annotations;
using Rake.ViewModels;
using Rake.ViewModels.Abstractions;
using Rake.Views;

namespace Rake;

[PublicAPI]
public sealed class ViewManager : IDataTemplate
{
    private readonly Dictionary<Type, Func<Control>> _map = [];

    public ViewManager() => Register<MainWindow, MainWindowViewModel>();

    public void Register<TView, TViewModel>()
        where TView : Control, new()
        where TViewModel : ViewModelBase => _map[typeof(TViewModel)] = () => new TView();

    public Control TryBindView(ViewModelBase viewModel)
    {
        var view = TryCreateView(viewModel);
        if (view is null)
            return new TextBlock
            {
                Text = $"Could not find view for {viewModel.GetType().FullName}",
            };
        view.DataContext ??= viewModel;
        BindActivation(viewModel, view);
        return view;
    }

    private Control? TryCreateView(ViewModelBase viewModel) =>
        _map.TryGetValue(viewModel.GetType(), out var factory) ? factory() : null;

    private static void BindActivation(ViewModelBase viewModel, Control control)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(control);

        control.Loaded += Loaded;
        control.Unloaded += Unloaded;

        return;

        void Loaded(object? sender, RoutedEventArgs e) => viewModel.Activate();

        void Unloaded(object? sender, RoutedEventArgs e)
        {
            viewModel.Deactivate();
            control.Loaded -= Loaded;
            control.Unloaded -= Unloaded;
        }
    }

    bool IDataTemplate.Match(object? data) => data is ViewModelBase;

    Control? ITemplate<object?, Control?>.Build(object? data) =>
        data is ViewModelBase viewModel ? TryBindView(viewModel) : null;
}
