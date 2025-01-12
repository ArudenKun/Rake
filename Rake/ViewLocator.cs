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
public sealed class ViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Func<Control>> _map = [];

    public ViewLocator()
    {
        Register<MainWindow, MainWindowViewModel>();
        Register<DashboardView, DashboardViewModel>();
        Register<UpdateView, UpdateViewModel>();
    }

    public void Register<TControl, TViewModel>()
        where TControl : Control, new()
        where TViewModel : ViewModelBase => _map[typeof(TViewModel)] = () => new TControl();

    public Control TryBindView(ViewModelBase viewModel)
    {
        var view = TryCreateView(viewModel);
        if (view is null)
            return new TextBlock
            {
                Text = $"Could not find view for {viewModel.GetType().FullName}",
            };
        view.DataContext ??= viewModel;
        BindEvents(view, viewModel);
        return view;
    }

    private Control? TryCreateView(ViewModelBase viewModel) =>
        _map.TryGetValue(viewModel.GetType(), out var factory) ? factory() : null;

    public static void BindEvents(Control control, ViewModelBase viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(control);

        control.Loaded += Loaded;
        control.Unloaded += Unloaded;

        return;

        void Loaded(object? sender, RoutedEventArgs e) =>
            viewModel.LoadedCommand.ExecuteAsync(null);

        void Unloaded(object? sender, RoutedEventArgs e)
        {
            // viewModel.DeactivateCommand.ExecuteAsync(null);
            control.Loaded -= Loaded;
            control.Unloaded -= Unloaded;
        }
    }

    bool IDataTemplate.Match(object? data) => data is ViewModelBase;

    Control? ITemplate<object?, Control?>.Build(object? data) =>
        data is ViewModelBase viewModel ? TryBindView(viewModel) : null;
}
