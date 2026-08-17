using System.Globalization;

namespace Rake.SourceGenerators.Extensions;

internal static class ParamExtensions
{
    public static string ToParameterString(this IParameterSymbol p) =>
        $"{p.Modifier(p.IsParams)}{p.Type()} {p.Name}{p.Default()}";

    public static string ToArgumentString(this IParameterSymbol p, bool castEnum = false) =>
        $"{p.Modifier()}{p.Cast(castEnum)}{p.Name}";

    #region Parts

    private static string Modifier(this IParameterSymbol p, bool isParams = false)
    {
        return p.RefKind switch
        {
            RefKind.In => "in ",
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.RefReadOnlyParameter => "ref readonly ",
            _ => isParams ? "params " : "",
        };
    }

    private static string Cast(this IParameterSymbol p, bool castEnum = false) =>
        castEnum && p.Type.IsEnum() ? "(int)" : "";

    private static string Type(this IParameterSymbol p)
    {
        string typeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Handle value types explicitly annotated as nullable (e.g. int?)
        // vs reference types with NRT annotations (e.g. string?)
        if (!p.Type.IsValueType && p.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return typeName.EndsWith("?") ? typeName : $"{typeName}?";
        }

        return typeName;
    }

    private static string Default(this IParameterSymbol p) =>
        p.HasExplicitDefaultValue ? $" = {p.Format(p.ExplicitDefaultValue)}" : "";

    private static string Format(this IParameterSymbol p, object? value)
    {
        if (value is null)
        {
            return p.NullableAnnotation == NullableAnnotation.NotAnnotated && !p.Type.IsValueType
                ? "default!"
                : "default";
        }

        return p.Type.Format(value);
    }

    private static string Format(this ITypeSymbol type, object value)
    {
        // Unwrap Nullable<T> first so underlying primitives format properly
        if (type.IsNullable() && type is INamedTypeSymbol namedType)
        {
            return namedType.TypeArguments.Single().Format(value);
        }

        return type.SpecialType switch
        {
            SpecialType.System_Char => SymbolDisplay.FormatLiteral((char)value, quote: true),
            SpecialType.System_String => SymbolDisplay.FormatLiteral((string)value, quote: true),
            SpecialType.System_Boolean => (bool)value ? "true" : "false",
            SpecialType.System_Byte => $"(byte){value}",
            SpecialType.System_SByte => $"(sbyte){value}",
            SpecialType.System_Int16 => $"(short){value}",
            SpecialType.System_UInt16 => $"(ushort){value}",
            SpecialType.System_Single => ((float)value).ToString("G9", CultureInfo.InvariantCulture)
                + "f",
            SpecialType.System_Double => ((double)value).ToString(
                "G17",
                CultureInfo.InvariantCulture
            ) + "d",
            SpecialType.System_Decimal => ((decimal)value).ToString(CultureInfo.InvariantCulture)
                + "m",
            SpecialType.System_UInt32 => $"{value}u",
            SpecialType.System_Int64 => $"{value}L",
            SpecialType.System_UInt64 => $"{value}UL",
            _ when type.IsEnum() => FormatEnum(type, value),
            _ => value.ToString() ?? "default",
        };
    }

    private static string FormatEnum(ITypeSymbol type, object value)
    {
        // Cast raw integral values to fully qualified enum members where possible
        string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"({typeName}){value}";
    }

    #endregion
}
