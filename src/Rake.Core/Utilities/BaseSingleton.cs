namespace Rake.Core.Utilities;

public abstract class BaseSingleton<T>
    where T : class, new()
{
    private static readonly Lazy<T> LazyInstance = new(new T());

    public static T Instance => LazyInstance.Value;
}
