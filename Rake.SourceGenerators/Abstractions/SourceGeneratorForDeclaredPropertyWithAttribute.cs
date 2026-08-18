namespace Rake.SourceGenerators.Abstractions;

internal abstract class SourceGeneratorForDeclaredPropertyWithAttribute
    : SourceGeneratorForDeclaredMemberWithAttribute<PropertyDeclarationSyntax>
{
    protected SourceGeneratorForDeclaredPropertyWithAttribute(Type attributeType)
        : base(attributeType) { }

    protected abstract (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        IPropertySymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    );

    protected sealed override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    ) => GenerateCode(compilation, node, (IPropertySymbol)symbol, attribute, options);
}

internal abstract class SourceGeneratorForDeclaredPropertyWithAttribute<TAttribute>
    : SourceGeneratorForDeclaredMemberWithAttribute<TAttribute, PropertyDeclarationSyntax>
    where TAttribute : Attribute
{
    protected abstract (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        IPropertySymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    );

    protected sealed override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    ) => GenerateCode(compilation, node, (IPropertySymbol)symbol, attribute, options);
}
