namespace Rake.SourceGenerators.Abstractions;

internal abstract class SourceGeneratorForDeclaredMethodWithAttribute
    : SourceGeneratorForDeclaredMemberWithAttribute<MethodDeclarationSyntax>
{
    protected SourceGeneratorForDeclaredMethodWithAttribute(Type attributeType)
        : base(attributeType) { }

    protected abstract (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        IMethodSymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    );

    protected sealed override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    ) => GenerateCode(compilation, node, (IMethodSymbol)symbol, attribute, options);
}

internal abstract class SourceGeneratorForDeclaredMethodWithAttribute<TAttribute>
    : SourceGeneratorForDeclaredMemberWithAttribute<TAttribute, MethodDeclarationSyntax>
    where TAttribute : Attribute
{
    protected abstract (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        IMethodSymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    );

    protected sealed override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    ) => GenerateCode(compilation, node, (IMethodSymbol)symbol, attribute, options);
}
