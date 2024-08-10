using System.Collections.Generic;
using System.Linq;
using System.Threading;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Rake.Generators.Extensions;

namespace Rake.Generators.Abstractions;

internal abstract class GeneratorForMemberWithAttribute<TDeclarationSyntax>
    : BaseGenerator,
        IIncrementalGenerator
    where TDeclarationSyntax : MemberDeclarationSyntax
{
    private Compilation _compilation = null!;

    protected abstract string Id { get; }
    protected virtual IEnumerable<FileWithName> StaticSources => [];
    protected override Compilation Compilation => _compilation;
    protected abstract string GetTargetAttribute();

    protected virtual bool? Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return null;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        foreach (var (name, source) in StaticSources)
            context.RegisterPostInitializationOutput(x => x.AddSource($"{name}.g.cs", source));

        var syntaxProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            GetTargetAttribute(),
            (node, token) =>
                Predicate(node, token) ?? node is TDeclarationSyntax { AttributeLists.Count: > 0 },
            (syntaxContext, _) => syntaxContext
        );
        context
            .CompilationProvider.Combine(syntaxProvider.Collect())
            .Combine(context.AnalyzerConfigOptionsProvider)
            .SelectAndReportExceptions(
                (tuple, ct) => Execute(tuple.Left.Left, tuple.Left.Right, tuple.Right, ct),
                context,
                Id
            )
            .AddSource(context);
    }

    private IEnumerable<FileWithName> Execute(
        Compilation compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> generatorAttributeSyntaxContexts,
        AnalyzerConfigOptionsProvider options,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        _compilation = compilation;

        var singles = generatorAttributeSyntaxContexts.Where(x => x.Attributes.Length == 1);
        var multiples = generatorAttributeSyntaxContexts.Where(x => x.Attributes.Length > 1);

        var fileWithNames = new List<FileWithName>();
        foreach (var single in singles)
        {
            var source = GenerateCode(
                single.TargetNode,
                single.TargetSymbol,
                single.Attributes[0],
                options.GlobalOptions
            );

            if (source.IsNullOfEmpty())
            {
                fileWithNames.Add(FileWithName.Empty);
            }

            fileWithNames.Add(new FileWithName(GenerateFilename(single.TargetSymbol), source));
        }

        foreach (var multiple in multiples)
        {
            var source = GenerateCode(
                multiple.TargetNode,
                multiple.TargetSymbol,
                multiple.Attributes,
                options.GlobalOptions
            );

            if (source.IsNullOfEmpty())
            {
                fileWithNames.Add(FileWithName.Empty);
            }

            fileWithNames.Add(new FileWithName(GenerateFilename(multiple.TargetSymbol), source));
        }

        return fileWithNames;
    }

    protected virtual string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }

    protected virtual string GenerateCode(
        SyntaxNode node,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        AnalyzerConfigOptions options
    )
    {
        return string.Empty;
    }
}
