using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Rake.Behaviors.Common;
using Rake.Core.Extensions;

namespace Rake.Behaviors;

public class ControlLoadedBehavior : DisposingTrigger<Control>
{
    protected override void OnAttached(CompositeDisposable disposables)
    {
        if (AssociatedObject is null)
        {
            return;
        }

        AssociatedObject.Loaded += AssociatedObjectOnLoaded;

        Disposable
            .Create(() => AssociatedObject.Loaded -= AssociatedObjectOnLoaded)
            .DisposeWith(disposables);
    }

    private void AssociatedObjectOnLoaded(object? sender, RoutedEventArgs e) =>
        Interaction.ExecuteActions(AssociatedObject, Actions, e);
}
