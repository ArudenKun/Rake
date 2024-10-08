﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using H;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Rake.Generators.Abstractions;
using Rake.Generators.Attributes;
using Rake.Generators.Extensions;
using Rake.Generators.Interfaces;
using Rake.Generators.Utilities;

namespace Rake.Generators;

[Generator]
internal sealed class PropertyGenerator : GeneratorForMember<BaseTypeDeclarationSyntax>
{
    protected override string Id => "PG";

    protected override IEnumerable<FileWithName> StaticSources { get; } =
        [
            new FileWithName(
                typeof(IView<>).FullName!,
                Resources.IView_cs.AsString().AddAutoGeneratedMessage()
            )
        ];

    protected override bool IsSyntaxTarget(SyntaxNode node, CancellationToken ct)
    {
        if (node is BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
        {
            return baseTypeDeclarationSyntax.Identifier.Text.EndsWith("ViewModel");
        }

        return false;
    }

    protected override IEnumerable<FileWithName> GenerateCodes(
        ImmutableArray<BaseTypeDeclarationSyntax> nodes,
        AnalyzerConfigOptions options
    )
    {
        var targetSymbol = Compilation.GetTypeByMetadataName(Meta.ObservableObject);

        var viewModelSymbols = GetAll<INamedTypeSymbol>(nodes)
            .Where(x => !x.IsAbstract)
            .Where(x => !x.HasAttribute<IgnoreAttribute>())
            .Where(x => x.Name.EndsWith("ViewModel"))
            .Where(x => x.IsOfBaseType(targetSymbol))
            .OrderBy(x => x.ToDisplayString())
            .ToArray();

        foreach (var viewModelSymbol in viewModelSymbols)
        {
            var viewSymbol = GetView(viewModelSymbol);
            if (viewSymbol is null)
                continue;

            var source = new SourceStringBuilder(viewSymbol);
            var viewInterfaceSymbol = Compilation.GetTypeByMetadataName(typeof(IView<>).FullName!);

            source.Line("#nullable enable");
            source.PartialTypeBlockBrace(
                $"{viewInterfaceSymbol?.Construct(viewModelSymbol).ToFullDisplayString()}",
                () =>
                {
                    source.Line($"public {viewModelSymbol.ToFullDisplayString()} ViewModel");
                    source.BlockBrace(() =>
                    {
                        source.Line(
                            $"get => DataContext as {viewModelSymbol.ToFullDisplayString()} ?? throw new global::System.ArgumentNullException(nameof(DataContext));"
                        );
                        source.Line("set => DataContext = value;");
                    });
                }
            );

            yield return new FileWithName(
                $"{viewSymbol.ToDisplayString()}.Property.g.cs",
                source.ToString()
            );
        }
    }

    private INamedTypeSymbol? GetView(ISymbol symbol)
    {
        var viewName = symbol.ToDisplayString().Replace("ViewModel", "View");
        var viewSymbol = Compilation.GetTypeByMetadataName(viewName);

        if (viewSymbol is not null)
            return viewSymbol;

        viewName = symbol.ToDisplayString().Replace(".ViewModels.", ".Views.");
        viewName = viewName.Remove(viewName.IndexOf("ViewModel", StringComparison.Ordinal));
        return Compilation.GetTypeByMetadataName(viewName);
    }
}
