namespace Rake.Core;

public sealed class Disposable(Action dispose) : IDisposable
{
    public static Disposable Create(Action dispose) => new(dispose);

    public void Dispose() => dispose();
}
