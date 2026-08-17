namespace Rake.SourceGenerators.Core;

public interface IParameterized<T>
    where T : BuilderBase<T>
{
    T Parent { get; }

    List<ParameterBuilder<T>> Parameters { get; }
}
