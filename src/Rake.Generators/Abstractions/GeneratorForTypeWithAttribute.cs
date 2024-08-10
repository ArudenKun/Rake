using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rake.Generators.Abstractions;

internal abstract class GeneratorForTypeWithAttribute<TAttribute> : GeneratorForTypeWithAttribute
    where TAttribute : Attribute
{
    protected override string GetTargetAttribute() => typeof(TAttribute).FullName!;
}

internal abstract class GeneratorForTypeWithAttribute
    : GeneratorForMemberWithAttribute<TypeDeclarationSyntax>
{
    protected sealed override string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    ) => GenerateCode(node, (INamedTypeSymbol)symbol, attribute, options);

    protected sealed override string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        AnalyzerConfigOptions options
    ) => GenerateCode(node, (INamedTypeSymbol)symbol, attributes, options);

    protected virtual string GenerateCode(
        SyntaxNode node,
        INamedTypeSymbol targetSymbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }

    protected virtual string GenerateCode(
        SyntaxNode node,
        INamedTypeSymbol targetSymbol,
        ImmutableArray<AttributeData> attributes,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }
}
