using System;

namespace Rake.Utilities;

public sealed class Disposable(Action dispose) : IDisposable
{
    public static Disposable Create(Action dispose) => new(dispose);

    public void Dispose() => dispose();
}
