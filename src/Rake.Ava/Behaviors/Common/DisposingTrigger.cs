using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Xaml.Interactivity;

namespace Rake.Behaviors.Common;

public abstract class DisposingTrigger<T> : Trigger<T>
    where T : AvaloniaObject
{
    private readonly CompositeDisposable _disposables = new();

    [RequiresUnreferencedCode("This functionality is not compatible with trimming.")]
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
