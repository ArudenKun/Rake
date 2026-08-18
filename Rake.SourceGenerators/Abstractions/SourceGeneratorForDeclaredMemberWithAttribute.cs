using System.Collections.Immutable;
using Rake.SourceGenerators.Extensions;

namespace Rake.SourceGenerators.Abstractions;

using GeneratorContext = IncrementalGeneratorInitializationContext;

public abstract class SourceGeneratorForDeclaredMemberWithAttribute<TAttribute, TDeclarationSyntax>
    : SourceGeneratorForDeclaredMemberWithAttribute<TDeclarationSyntax>
    where TAttribute : Attribute
    where TDeclarationSyntax : MemberDeclarationSyntax
{
    protected SourceGeneratorForDeclaredMemberWithAttribute()
        : base(typeof(TAttribute)) { }
}

public abstract class SourceGeneratorForDeclaredMemberWithAttribute<TDeclarationSyntax>
    : IIncrementalGenerator
    where TDeclarationSyntax : MemberDeclarationSyntax
{
    private readonly Type _attributeType;

    protected SourceGeneratorForDeclaredMemberWithAttribute(Type attributeType)
    {
        _attributeType = attributeType;
        AttributeType = attributeType.Name.AddSuffix("Attribute");
        AttributeName = attributeType.Name.TrimSuffix("Attribute");
    }

    protected string AttributeType { get; }

    protected string AttributeName { get; }

    protected virtual IEnumerable<(string Name, string Source)> StaticSources => [];

    public void Initialize(GeneratorContext context)
    {
        foreach (var (name, source) in StaticSources)
            context.RegisterPostInitializationOutput(x => x.AddSource($"{name}.g.cs", source));

        var attributeSourceNameFieldInfo = _attributeType.GetField("SourceName");
        var attributeSourceTextFieldInfo = _attributeType.GetField("SourceText");
        string sourceName = (string)attributeSourceNameFieldInfo.GetValue(null);
        string sourceText = (string)attributeSourceTextFieldInfo.GetValue(null);
        context.RegisterPostInitializationOutput(x =>
            x.AddSource($"{sourceName}.a.g.cs", sourceText)
        );

        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsSyntaxTarget,
            GetSyntaxTarget
        );
        var compilationProvider = context
            .CompilationProvider.Combine(syntaxProvider.Collect())
            .Combine(context.AnalyzerConfigOptionsProvider);
        context.RegisterImplementationSourceOutput(
            compilationProvider,
            (spc, provider) =>
                OnExecute(spc, provider.Left.Left, provider.Left.Right, provider.Right)
        );
    }

    private void OnExecute(
        SourceProductionContext spc,
        Compilation compilation,
        ImmutableArray<TDeclarationSyntax> nodes,
        AnalyzerConfigOptionsProvider options
    )
    {
        foreach (var node in nodes.Distinct())
        {
            if (spc.CancellationToken.IsCancellationRequested)
                return;

            var model = compilation.GetSemanticModel(node.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(Node(node));
            if (symbol is null)
                continue;

            var attribute = symbol
                .GetAttributes()
                .FirstOrDefault(x => x.AttributeClass?.Name == AttributeType);
            if (attribute is null)
                continue;

            var (generatedCode, error) = _GenerateCode(
                compilation,
                node,
                symbol,
                attribute,
                options.GlobalOptions
            );

            if (generatedCode is null)
            {
                var diagnosticId = !string.IsNullOrWhiteSpace(error?.Id)
                    ? error!.Id!
                    : AttributeName;
                var category = !string.IsNullOrWhiteSpace(error?.Category)
                    ? error!.Category!
                    : "Usage";
                var title = error?.Title ?? "Generation Error";
                var message = error?.Message ?? "An error occurred during code generation.";

                var descriptor = new DiagnosticDescriptor(
                    diagnosticId,
                    title,
                    message,
                    category,
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                );
                var location =
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                    ?? node.GetLocation();
                var diagnostic = Diagnostic.Create(descriptor, location);
                spc.ReportDiagnostic(diagnostic);
                continue;
            }

            spc.AddSource(GenerateFilename(symbol), generatedCode);
        }
    }

    protected virtual bool IsSyntaxTarget(SyntaxNode node, CancellationToken _)
    {
        return node is TDeclarationSyntax type && HasAttributeType();

        bool HasAttributeType()
        {
            if (type.AttributeLists.Count is 0)
                return false;

            foreach (var attributeList in type.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    var simpleName = name.Substring(name.LastIndexOf('.') + 1);
                    if (simpleName == AttributeName || simpleName == AttributeType)
                        return true;
                }
            }

            return false;
        }
    }

    protected virtual TDeclarationSyntax GetSyntaxTarget(
        GeneratorSyntaxContext context,
        CancellationToken _
    ) => (TDeclarationSyntax)context.Node;

    protected abstract (string? GeneratedCode, DiagnosticDetail? Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    );

    private (string? GeneratedCode, DiagnosticDetail? Error) _GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        try
        {
            return GenerateCode(compilation, node, symbol, attribute, options);
        }
        catch (Exception e)
        {
            return (null, InternalError(e));
        }

        static DiagnosticDetail InternalError(Exception e) =>
            new() { Title = "Internal Error", Message = e.Message };
    }

    protected const string Ext = ".g.cs";
    protected const int MaxFileLength = 255;

    protected virtual string GenerateFilename(ISymbol symbol)
    {
        var gn = $"{Format(symbol)}{Ext}";
        return gn;

        static string Format(ISymbol? symbol) =>
            string.Join(
                    "_",
                    (symbol?.ToDisplayString() ?? "Generated").Split(
                        RakeConsts.InvalidFileNameChars
                    )
                )
                .Truncate(MaxFileLength - Ext.Length);
    }

    protected virtual SyntaxNode Node(TDeclarationSyntax node) => node;
}
