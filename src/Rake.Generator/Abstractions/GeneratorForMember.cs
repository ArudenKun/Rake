using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Rake.Generator.Extensions;

namespace Rake.Generator.Abstractions;

internal abstract class GeneratorForMember<TDeclarationSyntax>
    : BaseGenerator,
        IIncrementalGenerator
    where TDeclarationSyntax : MemberDeclarationSyntax
{
    private Compilation _compilation = null!;
    protected abstract string Id { get; }
    protected virtual IEnumerable<FileWithName> StaticSources => [];
    protected override Compilation Compilation => _compilation;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        foreach (var staticSource in StaticSources)
            context.RegisterPostInitializationOutput(x =>
                x.AddSource($"{staticSource.Name}.g.cs", staticSource.Text)
            );

        var syntaxProvider = context
            .SyntaxProvider.CreateSyntaxProvider(IsSyntaxTarget, GetSyntaxTarget)
            .Where(x => x is not null);

        var providers = context
            .CompilationProvider.Combine(syntaxProvider.Collect())
            .Combine(context.AnalyzerConfigOptionsProvider);

        providers
            .SelectAndReportExceptions(
                (tuple, ct) => OnExecute(tuple.Left.Left, tuple.Left.Right, tuple.Right, ct),
                context,
                Id
            )
            .AddSource(context);
    }

    protected virtual bool IsSyntaxTarget(SyntaxNode node, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return node is TDeclarationSyntax;
    }

    protected virtual TDeclarationSyntax GetSyntaxTarget(
        GeneratorSyntaxContext context,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        return (TDeclarationSyntax)context.Node;
    }

    private IEnumerable<FileWithName> OnExecute(
        Compilation compilation,
        ImmutableArray<TDeclarationSyntax> nodes,
        AnalyzerConfigOptionsProvider options,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        _compilation = compilation;

        try
        {
            var fileWithNames = new List<FileWithName>
            {
                GenerateCode(nodes, options.GlobalOptions)
            };

            fileWithNames.AddRange(GenerateCodes(nodes, options.GlobalOptions));

            return fileWithNames;
        }
        catch (Exception)
        {
            return [];
        }
    }

    protected virtual FileWithName GenerateCode(
        ImmutableArray<TDeclarationSyntax> nodes,
        AnalyzerConfigOptions options
    )
    {
        return FileWithName.Empty;
    }

    protected virtual IEnumerable<FileWithName> GenerateCodes(
        ImmutableArray<TDeclarationSyntax> nodes,
        AnalyzerConfigOptions options
    )
    {
        return [];
    }
}
