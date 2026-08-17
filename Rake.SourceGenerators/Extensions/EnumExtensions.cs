namespace Rake.SourceGenerators.Extensions;

internal static class EnumExtensions
{
    public static INamedTypeSymbol? GetEnumType(this Compilation compilation, string name)
    {
        // Avoid LINQ allocations in compiler-bound loops
        INamedTypeSymbol? found = null;

        foreach (var symbol in compilation.GetSymbolsWithName(name, SymbolFilter.Type))
        {
            if (symbol is INamedTypeSymbol typeSymbol && typeSymbol.TypeKind == TypeKind.Enum)
            {
                if (found is not null)
                {
                    // Ambiguous match (multiple enums share the same name)
                    return null;
                }

                found = typeSymbol;
            }
        }

        return found;
    }

    public static string GetEnumValue(this ITypeSymbol type, object value)
    {
        string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (type is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (
                    member is IFieldSymbol field
                    && field.HasConstantValue
                    && ConstantEquals(field.ConstantValue, value)
                )
                {
                    return $"{typeName}.{field.Name}";
                }
            }
        }

        // Fallback for [Flags] combinations or underlying values without a named constant
        return $"({typeName}){value}";
    }

    private static bool ConstantEquals(object? a, object? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;

        // Boxing numeric values from Roslyn symbols often leads to mismatched primitive types
        // (e.g. byte vs int), so convert to systemic uint64 for unified comparison.
        try
        {
            return Convert.ToUInt64(a) == Convert.ToUInt64(b);
        }
        catch
        {
            return Equals(a, b);
        }
    }
}
