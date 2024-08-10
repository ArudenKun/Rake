using System.Collections.Generic;
using H.Generators.Extensions;

namespace Rake.Generators.Extensions;

internal static class IncrementalValuesProviderExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <param name="source"></param>
    public static void AddSource(
        this IncrementalValueProvider<IEnumerable<FileWithName>> source,
        IncrementalGeneratorInitializationContext context
    )
    {
        source.SelectMany(static (x, _) => x).AddSource(context);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <param name="source"></param>
    public static void AddSource(
        this IncrementalValuesProvider<IEnumerable<FileWithName>> source,
        IncrementalGeneratorInitializationContext context
    )
    {
        source.SelectMany(static (x, _) => x).AddSource(context);
    }
}
