using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rake.Generators.Extensions;

namespace Rake.Generators.Abstractions;

internal abstract class BaseGenerator
{
    private const string Ext = ".g.cs";
    private const int MaxFileLength = 255;

    protected abstract Compilation Compilation { get; }

    protected virtual string GenerateFilename(ISymbol symbol)
    {
        var gn = $"{Format().SanitizeName()}{Ext}";
        return gn;

        string Format() =>
            string.Join(
                    "_",
                    $"{symbol}.{GetType().Name.Replace("Generator", "")}".Split(
                        Path.GetInvalidPathChars()
                    )
                )
                .Truncate(MaxFileLength - Ext.Length);
    }

    protected IEnumerable<TSymbol> GetAll<TSymbol>(IEnumerable<SyntaxNode> syntaxNodes)
        where TSymbol : ISymbol
    {
        foreach (var syntaxNode in syntaxNodes)
            if (syntaxNode is FieldDeclarationSyntax fieldDeclaration)
            {
                var semanticModel = Compilation.GetSemanticModel(fieldDeclaration.SyntaxTree);

                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    if (semanticModel.GetDeclaredSymbol(variable) is not TSymbol symbol)
                        continue;

                    yield return symbol;
                }
            }
            else
            {
                var semanticModel = Compilation.GetSemanticModel(syntaxNode.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(syntaxNode) is not TSymbol symbol)
                    continue;

                yield return symbol;
            }
    }
}
