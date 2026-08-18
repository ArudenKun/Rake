using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Rake.SourceGenerators.UtilityGenerator;

[Generator]
internal class AttributeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Syntactic filtering: find classes inheriting from Attribute
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => IsSyntaxTarget(node),
                transform: static (ctx, _) => GetTarget(ctx)
            )
            .Where(static m => m is not null)!;

        // Combine with compilation context
        IncrementalValueProvider<(
            Compilation Compilation,
            ImmutableArray<ClassDeclarationSyntax> Classes
        )> compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Register output generation
        context.RegisterSourceOutput(
            compilationAndClasses,
            static (spc, source) => Execute(source.Compilation, source.Classes, spc)
        );
    }

    private static bool IsSyntaxTarget(SyntaxNode syntax)
    {
        return syntax is ClassDeclarationSyntax { BaseList: not null };
    }

    private static ClassDeclarationSyntax? GetTarget(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not { } symbol)
            return null;

        // Verify inheritance from System.Attribute
        INamedTypeSymbol? current = symbol.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == "System.Attribute")
            {
                return classDecl;
            }

            current = current.BaseType;
        }

        return null;
    }

    private static void Execute(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classes,
        SourceProductionContext context
    )
    {
        if (classes.IsDefaultOrEmpty)
            return;

        // Attributes you want to strip out from the source text (if any)
        var attributesToRemove = new HashSet<string>(StringComparer.Ordinal)
        {
            "Required",
            "RequiredAttribute",
            "GenerateFactory",
            "GenerateFactoryAttribute",
        };

        foreach (var classDecl in classes.Distinct())
        {
            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDecl) is not { } symbol)
                continue;

            string className = symbol.Name;
            string shortName = className.EndsWith("Attribute", StringComparison.Ordinal)
                ? className.Substring(0, className.Length - "Attribute".Length)
                : className;

            string namespaceName = symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : symbol.ContainingNamespace.ToDisplayString();

            // 1. Build the modified source string with 'partial' stripped out
            string cleanedSourceText = BuildCleanedClassSource(
                classDecl,
                namespaceName,
                className,
                attributesToRemove
            );

            // 2. Generate partial class augmenting the attribute with SourceName and SourceText constants
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName};");
                sb.AppendLine();
            }

            sb.AppendLine($"partial class {className}");
            sb.AppendLine("{");
            sb.AppendLine($"    public const string SourceName = \"{className}\";");
            sb.AppendLine(
                $"    public const string SourceText = @\"{cleanedSourceText.Replace("\"", "\"\"")}\";"
            );
            sb.AppendLine("}");

            context.AddSource($"{shortName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static string BuildCleanedClassSource(
        ClassDeclarationSyntax classDecl,
        string namespaceName,
        string className,
        HashSet<string> attributesToRemove
    )
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
        }

        // Check if "using System;" is already present anywhere in the file's usings
        bool hasSystemUsing = false;
        if (classDecl.SyntaxTree.GetRoot() is CompilationUnitSyntax rootNode)
        {
            hasSystemUsing = rootNode.Usings.Any(u => u.Name?.ToString() == "System");
        }

        if (!hasSystemUsing)
        {
            sb.AppendLine("    using System;");
            sb.AppendLine();
        }

        // Filter and write out remaining custom attributes
        foreach (var attrGroup in classDecl.AttributeLists)
        {
            var filteredAttrs = attrGroup
                .Attributes.Where(a => !attributesToRemove.Contains(a.Name.ToString()))
                .ToList();

            if (filteredAttrs.Count > 0)
            {
                sb.AppendLine($"    [{string.Join(", ", filteredAttrs)}]");
            }
        }

        // Strip out the 'partial' keyword from modifiers
        var modifiers = classDecl.Modifiers.Where(m => m.Text != "partial").Select(m => m.Text);

        string modifierStr = string.Join(" ", modifiers);
        if (!string.IsNullOrWhiteSpace(modifierStr))
        {
            sb.Append($"    {modifierStr} ");
        }

        // Output class signature and original body syntax
        sb.AppendLine($"class {className}{classDecl.TypeParameterList}{classDecl.BaseList}");
        sb.AppendLine(classDecl.Members.FullOptionToFullString());

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine("}");
        }

        return sb.ToString().Trim();
    }
}

internal static class SyntaxExtensions
{
    public static string FullOptionToFullString(this SyntaxList<MemberDeclarationSyntax> members)
    {
        var sb = new StringBuilder();
        sb.AppendLine("    {");
        foreach (var member in members)
        {
            sb.AppendLine($"        {member.ToFullString().Trim()}");
        }

        sb.AppendLine("    }");
        return sb.ToString();
    }
}
