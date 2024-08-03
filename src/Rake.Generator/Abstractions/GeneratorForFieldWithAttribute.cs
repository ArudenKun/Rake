using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rake.Generator.Abstractions;

internal abstract class GeneratorForFieldWithAttribute<TAttribute> : GeneratorForFieldWithAttribute
    where TAttribute : Attribute
{
    protected override string GetTargetAttribute() => typeof(TAttribute).FullName!;
}

internal abstract class GeneratorForFieldWithAttribute
    : GeneratorForMemberWithAttribute<FieldDeclarationSyntax>
{
    protected sealed override string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    ) => GenerateCode(node, (IFieldSymbol)symbol, attribute, options);

    protected sealed override string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        AnalyzerConfigOptions options
    ) => GenerateCode(node, (IFieldSymbol)symbol, attributes, options);

    protected virtual string GenerateCode(
        SyntaxNode node,
        IFieldSymbol targetSymbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }

    protected virtual string GenerateCode(
        SyntaxNode node,
        IFieldSymbol targetSymbol,
        ImmutableArray<AttributeData> attributes,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }
}
