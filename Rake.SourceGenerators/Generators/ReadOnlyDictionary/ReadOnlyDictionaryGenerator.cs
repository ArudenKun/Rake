using Rake.SourceGenerators.Abstractions;
using Rake.SourceGenerators.Attributes;
using Rake.SourceGenerators.Builder;
using Rake.SourceGenerators.Extensions;

namespace Rake.SourceGenerators.Generators.ReadOnlyDictionary;

[Generator]
internal class ReadOnlyDictionaryGenerator
    : SourceGeneratorForDeclaredTypeWithAttribute<ReadOnlyDictionaryAttribute>
{
    protected override (string? GeneratedCode, DiagnosticDetail? Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        INamedTypeSymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        var builder = CodeBuilder.Create(symbol);

        var model = attribute.GetReadOnlyDictionaryAttributeModel();

        var properties = symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                !x.IsStatic && x.HasGetter() && x.DeclaredAccessibility == Accessibility.Public
            )
            .ToArray();

        // Map all properties to their deduplicated string aliases based on options
        var processedCases = new HashSet<string>();
        var aliasMap = properties
            .Select(p => (Property: p, Aliases: GetUniqueAliases(p, model, processedCases)))
            .Where(x => x.Aliases.Length > 0)
            .ToArray();

        var totalKeysCount = aliasMap.Sum(x => x.Aliases.Length);
        const string interfaceType =
            "global::System.Collections.Generic.IReadOnlyDictionary<string, object?>";

        builder.AddInterface(interfaceType);

        // --- Properties ---

        // Count: returns total unique key mappings
        builder
            .AddProperty("Count", Accessibility.Public)
            .SetType("int")
            .WithGetterExpression($"{totalKeysCount}");

        // Keys: yields each registered key string
        builder
            .AddProperty("Keys", Accessibility.Public)
            .SetType("global::System.Collections.Generic.IEnumerable<string>")
            .WithGetterExpression("GetKeysInternal()");

        // Values: yields each current property value
        builder
            .AddProperty("Values", Accessibility.Public)
            .SetType("global::System.Collections.Generic.IEnumerable<object?>")
            .WithGetterExpression("GetValuesInternal()");

        // Indexer: evaluates switch expression dynamically
        builder
            .AddProperty("this[string key]", Accessibility.Public)
            .SetType("object?")
            .WithGetterExpression(
                "TryGetValue(key, out var val) ? val : throw new global::System.Collections.Generic.KeyNotFoundException($\"Key '{key}' was not found.\")"
            );

        // --- Methods ---

        // TryGetValue Method
        builder
            .AddMethod("TryGetValue", Accessibility.Public)
            .WithReturnType("bool")
            .AddParameter("string", "key")
            .AddParameter("out object?", "value")
            .WithBody(writer =>
            {
                using (writer.Block("switch (key)"))
                {
                    foreach (var (property, aliases) in aliasMap)
                    {
                        foreach (var alias in aliases)
                        {
                            writer.AppendLine($"case \"{alias}\":");
                        }

                        writer.AppendLine($"    value = {property.Name}; return true;");
                    }

                    writer.AppendLine("default:");
                    writer.AppendLine("    value = null; return false;");
                }
            });

        // ContainsKey Method
        builder
            .AddMethod("ContainsKey", Accessibility.Public)
            .WithReturnType("bool")
            .AddParameter("string", "key")
            .WithExpressionBody("TryGetValue(key, out _)");

        // Generic GetEnumerator: uses yield return directly
        builder
            .AddMethod("GetEnumerator", Accessibility.Public)
            .WithReturnType(
                "global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, object?>>"
            )
            .WithBody(writer =>
            {
                foreach (var (property, aliases) in aliasMap)
                {
                    foreach (var alias in aliases)
                    {
                        writer.AppendLine(
                            $"yield return new global::System.Collections.Generic.KeyValuePair<string, object?>(\"{alias}\", {property.Name});"
                        );
                    }
                }
            });

        // Non-generic GetEnumerator
        builder
            .AddMethod("global::System.Collections.IEnumerable.GetEnumerator")
            .WithExplicitInterface()
            .WithReturnType("global::System.Collections.IEnumerator")
            .WithExpressionBody("GetEnumerator()");

        // Helper: Yield Keys
        builder
            .AddMethod("GetKeysInternal", Accessibility.Private)
            .WithReturnType("global::System.Collections.Generic.IEnumerable<string>")
            .WithBody(writer =>
            {
                foreach (var (_, aliases) in aliasMap)
                {
                    foreach (var alias in aliases)
                    {
                        writer.AppendLine($"yield return \"{alias}\";");
                    }
                }
            });

        // Helper: Yield Values
        builder
            .AddMethod("GetValuesInternal", Accessibility.Private)
            .WithReturnType("global::System.Collections.Generic.IEnumerable<object?>")
            .WithBody(writer =>
            {
                foreach (var (property, aliases) in aliasMap)
                {
                    foreach (var _ in aliases)
                    {
                        writer.AppendLine($"yield return {property.Name};");
                    }
                }
            });

        var code = $"""
            #nullable enable
            {builder.Build()}
            """;

        return (code, null);
    }

    private static string[] GetUniqueAliases(
        IPropertySymbol property,
        ReadOnlyDictionaryAttribute.Model model,
        HashSet<string> processedCases
    )
    {
        var originalName = property.Name;
        var aliases = new List<string>();

        if (model.IncludeJsonPropertyNameAttribute)
        {
            foreach (var attr in property.GetAttributes())
            {
                if (
                    attr.AttributeClass?.ToDisplayString()
                        == "System.Text.Json.Serialization.JsonPropertyNameAttribute"
                    && attr.ConstructorArguments.Length > 0
                    && attr.ConstructorArguments[0].Value is string jsonName
                    && !string.IsNullOrWhiteSpace(jsonName)
                )
                {
                    aliases.Add(jsonName);
                }
            }
        }

        // Original property name is always included as the primary key
        aliases.Add(originalName);

        if (model.IncludeUnderscore)
            aliases.Add(originalName.Underscore());
        if (model.IncludePascalize)
            aliases.Add(originalName.Pascalize());
        if (model.IncludeCamelize)
            aliases.Add(originalName.Camelize());
        if (model.IncludeKebaberize)
            aliases.Add(originalName.Kebaberize());
        if (model.IncludeLower)
            aliases.Add(originalName.ToLowerInvariant());
        if (model.IncludeUpper)
            aliases.Add(originalName.ToUpperInvariant());

        return [.. aliases.Where(processedCases.Add)];
    }
}
