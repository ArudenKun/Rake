using System.Collections.Immutable;

namespace Rake.SourceGenerators.Extensions;

internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat FullNameFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
            SymbolDisplayGlobalNamespaceStyle.Omitted
        );

    public static string ToFullDisplayString(this ISymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string Namespace(this ISymbol symbol) =>
        symbol.ContainingNamespace?.ToDisplayString(FullNameFormat) ?? string.Empty;

    public static bool HasNamespace(this ISymbol symbol) =>
        symbol.ContainingNamespace is { IsGlobalNamespace: false };

    public static string? NamespaceOrNull(this ISymbol symbol) =>
        symbol.HasNamespace() ? symbol.Namespace() : null;

    public static string FullName(this ISymbol symbol) => symbol.ToDisplayString(FullNameFormat);

    public static string GlobalName(this ISymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string? GetNamespaceDeclaration(this ISymbol symbol)
    {
        var ns = symbol.NamespaceOrNull();
        return ns is null ? null : $"namespace {ns};\n";
    }

    public static INamedTypeSymbol? OuterType(this ISymbol symbol)
    {
        var current = symbol.ContainingType;
        if (current is null)
        {
            return symbol as INamedTypeSymbol;
        }

        while (current.ContainingType is not null)
        {
            current = current.ContainingType;
        }

        return current;
    }

    public static string ClassDef(this INamedTypeSymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    public static string? ClassPath(this INamedTypeSymbol symbol) =>
        symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath;

    public static string GeneratePartialClass(
        this INamedTypeSymbol symbol,
        IEnumerable<string>? content,
        IEnumerable<string>? usings = null
    )
    {
        var usingsHeader = usings is not null ? string.Join("\n", usings) : string.Empty;
        var body = content is not null ? string.Join("\n    ", content) : string.Empty;

        // Evaluate IsRecord first, as Roslyn treats records as Class or Struct under TypeKind
        var typeKind = symbol switch
        {
            { IsRecord: true, IsValueType: true } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Interface } => "interface",
            { TypeKind: TypeKind.Struct } => "struct",
            _ => "class",
        };

        return $$"""
            {{usingsHeader}}

            {{symbol.GetNamespaceDeclaration()}}
            {{symbol.GetDeclaredAccessibility()}} partial {{typeKind}} {{symbol.ClassDef()}}
            {
                {{body}}
            }
            """.TrimStart();
    }

    public static string Scope(this ISymbol symbol) => symbol.GetDeclaredAccessibility();

    public static string GetDeclaredAccessibility(this ISymbol symbol) =>
        SyntaxFacts.GetText(symbol.DeclaredAccessibility);

    public static bool Is(this ISymbol? symbol, ISymbol? other) =>
        SymbolEqualityComparer.Default.Equals(symbol, other);

    public static T[] Args<T>(this ImmutableArray<TypedConstant> values)
    {
        if (values.IsDefaultOrEmpty)
        {
            return [];
        }

        var result = new T[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i].Value is T typedVal)
            {
                result[i] = typedVal;
            }
        }

        return result;
    }

    public static bool HasGetter(this IPropertySymbol symbol) => symbol.GetMethod is not null;

    public static bool HasSetter(this IPropertySymbol symbol) => symbol.SetMethod is not null;

    public static bool IsOrInherits(this ITypeSymbol type, ITypeSymbol baseType)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
        }

        return false;
    }

    public static bool IsOrInherits(this ITypeSymbol type, string baseType)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.Name == baseType)
                return true;
        }

        return false;
    }

    public static bool IsEnum(this ITypeSymbol type) => type.TypeKind is TypeKind.Enum;

    public static bool IsClass(this ITypeSymbol type) => type.TypeKind is TypeKind.Class;

    public static bool IsNullable(this ITypeSymbol type) =>
        type.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T;
}
