using R3;

namespace Rake.Core.Extensions;

public static class DisposableExtensions
{
    public static void DisposeAll(this IEnumerable<IDisposable> disposables)
    {
        var exceptions = default(List<Exception>);

        foreach (var disposable in disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions is { Count: not 0 })
            throw new AggregateException(exceptions);
    }

    /// <summary>
    /// Ensures the provided disposable is disposed with the specified <see cref="CompositeDisposable"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the disposable.
    /// </typeparam>
    /// <param name="disposable">
    /// The disposable we are going to want to be disposed by the DisposableCollector.
    /// </param>
    /// <param name="disposables">
    /// The <see cref="CompositeDisposable"/> to which <paramref name="disposable"/> will be added.
    /// </param>
    /// <returns>
    /// The disposable.
    /// </returns>
    public static T DisposeWith<T>(this T disposable, CompositeDisposable disposables)
        where T : IDisposable
    {
        disposables.Add(disposable);
        return disposable;
    }
}
