using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rake.Generators.Abstractions;

internal abstract class GeneratorForPropertyWithAttribute<TAttribute>
    : GeneratorForPropertyWithAttribute
    where TAttribute : Attribute
{
    protected override string GetTargetAttribute() => typeof(TAttribute).FullName!;
}

internal abstract class GeneratorForPropertyWithAttribute
    : GeneratorForMemberWithAttribute<PropertyDeclarationSyntax>
{
    protected sealed override string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    ) => GenerateCode(node, (IPropertySymbol)symbol, attribute, options);

    protected sealed override string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        AnalyzerConfigOptions options
    ) => GenerateCode(node, (IPropertySymbol)symbol, attributes, options);

    protected virtual string GenerateCode(
        SyntaxNode node,
        IPropertySymbol targetSymbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }

    protected virtual string GenerateCode(
        SyntaxNode node,
        IPropertySymbol targetSymbol,
        ImmutableArray<AttributeData> attributes,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }
}
