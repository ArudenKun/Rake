using System.Collections.Immutable;
using Rake.SourceGenerators.Abstractions;
using Rake.SourceGenerators.Attributes;
using Rake.SourceGenerators.Builder;
using Rake.SourceGenerators.Extensions;

namespace Rake.SourceGenerators.Generators.ViewModelLocator;

[Generator]
internal class ViewModelLocatorGenerator
    : SourceGeneratorForDeclaredTypeWithAttribute<ViewModelLocatorAttribute>
{
    private const string DefaultSuffix = "ViewModel";

    protected override (string? GeneratedCode, DiagnosticDetail? Error) GenerateCode(
        Compilation compilation,
        SyntaxNode node,
        INamedTypeSymbol symbol,
        AttributeData attribute,
        AnalyzerConfigOptions options
    )
    {
        // 1. Ensure the decorated target class is valid
        if (
            symbol.ContainingNamespace is null
            || !symbol.ContainingSymbol.Equals(
                symbol.ContainingNamespace,
                SymbolEqualityComparer.Default
            )
        )
        {
            return (
                null,
                new DiagnosticDetail
                {
                    Id = "NamespaceError",
                    Category = "Namespace",
                    Title = "Namespace is missing",
                    Message = "Namespace is null",
                }
            ); // Skip nested classes
        }

        var isPartial = symbol
            .DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial)
        {
            return (
                null,
                new DiagnosticDetail { Message = $"{symbol.FullName()} must be partial" }
            ); // Class must be partial
        }

        // 2. Parse Attribute parameters with safe null fallbacks
        var suffix = DefaultSuffix;
        var baseTypes = ImmutableArray<ITypeSymbol>.Empty;

        var model = attribute.GetViewModelLocatorAttributeModel();

        // Default to "ViewModel" if ViewModelSuffix is null, empty, or whitespace
        if (!string.IsNullOrWhiteSpace(model.ViewModelSuffix))
        {
            suffix = model.ViewModelSuffix;
        }

        if (!model.BaseTypes.IsDefaultOrEmpty)
        {
            // Filter out any unresolvable/null types from the array
            baseTypes = [.. model.BaseTypes.Where(b => b is not null)];
        }

        // 3. Scan compilation for matching candidate ViewModels
        var discoveredViewModels = new List<INamedTypeSymbol>();
        var seenTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        CollectViewModels(
            compilation.GlobalNamespace,
            compilation,
            symbol,
            suffix,
            baseTypes,
            discoveredViewModels,
            seenTypes
        );

        discoveredViewModels.Sort(
            (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal)
        );

        var builder = CodeBuilder.Create(symbol);

        builder.WithAccessModifier(Accessibility.NotApplicable);

        builder.DontSortFieldsByName();

        builder.AddNamespaceImport("System");
        builder.AddNamespaceImport("Microsoft.Extensions.DependencyInjection");

        builder
            .AddConstructor(Accessibility.Public)
            .AddParameter("IServiceProvider", "serviceProvider")
            .WithBody(writer => writer.AppendLine("_serviceProvider = serviceProvider;"));

        builder
            .AddField("_serviceProvider", Accessibility.Private)
            .SetType<IServiceProvider>()
            .WithReadonlyValue();

        var propertyNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var vm in discoveredViewModels)
        {
            var fullTypeName = vm.ToFullDisplayString();
            var propertyName = GetPropertyName(vm.Name, suffix);

            // Guard against duplicate property identifiers
            if (!propertyNames.Add(propertyName))
            {
                propertyName = vm.Name;
                propertyNames.Add(propertyName);
            }

            builder
                .AddProperty(propertyName)
                .SetType(fullTypeName)
                .WithGetterExpression($"_serviceProvider.GetRequiredService<{fullTypeName}>()");
        }

        return (builder.Build(), null);
    }

    private static void CollectViewModels(
        INamespaceSymbol? namespaceSymbol,
        Compilation compilation,
        INamedTypeSymbol locatorSymbol,
        string suffix,
        ImmutableArray<ITypeSymbol> baseTypes,
        List<INamedTypeSymbol> result,
        HashSet<INamedTypeSymbol> seenTypes
    )
    {
        if (namespaceSymbol is null)
            return;

        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            if (type is not null)
            {
                CollectViewModels(
                    type,
                    compilation,
                    locatorSymbol,
                    suffix,
                    baseTypes,
                    result,
                    seenTypes
                );
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            if (childNamespace is not null)
            {
                CollectViewModels(
                    childNamespace,
                    compilation,
                    locatorSymbol,
                    suffix,
                    baseTypes,
                    result,
                    seenTypes
                );
            }
        }
    }

    private static void CollectViewModels(
        INamedTypeSymbol? typeSymbol,
        Compilation compilation,
        INamedTypeSymbol locatorSymbol,
        string suffix,
        ImmutableArray<ITypeSymbol> baseTypes,
        List<INamedTypeSymbol> result,
        HashSet<INamedTypeSymbol> seenTypes
    )
    {
        if (typeSymbol is null)
        {
            return;
        }

        if (
            IsViewModelCandidate(typeSymbol, compilation, locatorSymbol, suffix, baseTypes)
            && seenTypes.Add(typeSymbol)
        )
        {
            result.Add(typeSymbol);
        }

        foreach (var nested in typeSymbol.GetTypeMembers())
        {
            if (nested is not null)
            {
                CollectViewModels(
                    nested,
                    compilation,
                    locatorSymbol,
                    suffix,
                    baseTypes,
                    result,
                    seenTypes
                );
            }
        }
    }

    private static bool IsViewModelCandidate(
        INamedTypeSymbol symbol,
        Compilation compilation,
        INamedTypeSymbol locatorSymbol,
        string suffix,
        ImmutableArray<ITypeSymbol> baseTypes
    )
    {
        if (
            symbol.TypeKind != TypeKind.Class
            || symbol.IsAbstract
            || symbol.IsStatic
            || symbol.IsGenericType
        )
        {
            return false;
        }

        if (!compilation.IsSymbolAccessibleWithin(symbol, locatorSymbol))
        {
            return false;
        }

        var matchesSuffix =
            !string.IsNullOrEmpty(suffix) && symbol.Name.EndsWith(suffix, StringComparison.Ordinal);

        // If BaseTypes is omitted/empty: match solely by ViewModelSuffix
        if (baseTypes.IsEmpty)
        {
            return matchesSuffix;
        }

        // If BaseTypes is specified: candidate MUST match the suffix AND inherit/implement one of the BaseTypes
        if (!matchesSuffix)
        {
            return false;
        }

        for (var current = symbol.BaseType; current is not null; current = current.BaseType)
        {
            var target = current;
            if (
                baseTypes.Any(b =>
                    b is not null && SymbolEqualityComparer.Default.Equals(b, target)
                )
            )
            {
                return true;
            }
        }

        foreach (var iface in symbol.AllInterfaces)
        {
            if (
                iface is not null
                && baseTypes.Any(b =>
                    b is not null && SymbolEqualityComparer.Default.Equals(b, iface)
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    private static string GetPropertyName(string vmName, string suffix)
    {
        if (
            !string.IsNullOrEmpty(suffix)
            && vmName.EndsWith(suffix, StringComparison.Ordinal)
            && vmName.Length > suffix.Length
        )
        {
            return vmName.Substring(0, vmName.Length - suffix.Length);
        }

        return vmName;
    }
}
