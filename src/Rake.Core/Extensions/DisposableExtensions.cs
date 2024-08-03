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

        if (exceptions?.Count > 0)
            throw new AggregateException(exceptions);
    }
}
