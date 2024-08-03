using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Xaml.Interactivity;

namespace Rake.Behaviors.Common;

public abstract class DisposingTrigger<TControl> : Trigger<TControl>
    where TControl : AvaloniaObject
{
    private readonly CompositeDisposable _disposables = new();

    protected override void OnAttached()
    {
        base.OnAttached();

        OnAttached(_disposables);
    }

    protected abstract void OnAttached(CompositeDisposable disposables);

    protected override void OnDetaching()
    {
        base.OnDetaching();

        _disposables.Dispose();
    }
}
