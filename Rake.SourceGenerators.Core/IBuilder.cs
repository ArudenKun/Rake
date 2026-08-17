using Rake.SourceGenerators.Core.Internals;

namespace Rake.SourceGenerators.Core;

internal interface IBuilder
{
    void Write(in CodeWriter writer);
}
