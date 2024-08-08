using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Rake.Behaviors.Common;
using Rake.Core.Extensions;
using ReactiveMarbles.ObservableEvents;

namespace Rake.Behaviors;

public class ControlLoadedTrigger : DisposingTrigger<Control>
{
    protected override void OnAttached(CompositeDisposable disposables)
    {
        AssociatedObject
            ?.Events()
            .Loaded.Subscribe(e => Interaction.ExecuteActions(AssociatedObject, Actions, e))
            .DisposeWith(disposables);
    }
}
