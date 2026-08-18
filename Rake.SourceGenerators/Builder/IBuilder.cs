using Rake.SourceGenerators.Builder.Internals;

namespace Rake.SourceGenerators.Builder;

internal interface IBuilder
{
    void Write(in CodeWriter writer);
}
