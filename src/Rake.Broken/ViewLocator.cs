using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Rake.Generator.Attributes;
using Rake.ViewModels.Common;

namespace Rake;

[StaticViewLocator]
public sealed partial class ViewLocator
{
    public Control? Build(object? viewModel)
    {
        if (viewModel is null)
            return null;

        var viewModelType = viewModel.GetType();

        if (!ViewMap.TryGetValue(viewModelType, out var factory))
        {
            return new TextBlock { Text = $"No view registered for {viewModelType.FullName}" };
        }

        var control = factory(viewModel);
        control.DataContext = viewModel;
        RegisterViewModelEvents(viewModel, control);
        return control;
    }

    public bool Match(object? data) => data is INotifyPropertyChanged;

    private static void RegisterViewModelEvents(object viewModel, Control control)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (viewModel is not IViewModelEvents viewModelEvents)
            return;

        control = control ?? throw new ArgumentNullException(nameof(control));

        control.Loaded += Loaded;
        control.Unloaded += Unloaded;
        control.AttachedToVisualTree += AttachedToVisualTree;
        control.DetachedFromVisualTree += DetachedFromVisualTree;

        return;

        void Loaded(object? sender, RoutedEventArgs e)
        {
            viewModelEvents?.Loaded();
        }

        void Unloaded(object? sender, RoutedEventArgs e)
        {
            viewModelEvents?.Unloaded();

            control.Loaded -= Loaded;
            control.Unloaded -= Unloaded;
        }

        void AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            viewModelEvents.AttachedToVisualTree();
        }

        void DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            viewModelEvents.DetachedFromVisualTree();

            control.AttachedToVisualTree -= AttachedToVisualTree;
            control.DetachedFromVisualTree -= DetachedFromVisualTree;
        }
    }
}
