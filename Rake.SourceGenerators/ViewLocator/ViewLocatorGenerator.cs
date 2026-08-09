// using CodeGenHelpers;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.Diagnostics;
// using SourceWeaver;
// using SourceWeaver.Generators;
//
// namespace Rake.SourceGenerators.ViewLocator;
//
// public class ViewLocatorGenerator
//     : SourceGeneratorForDeclaredTypeWithAttribute<ViewLocatorAttribute>
// {
//     protected override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
//         Compilation compilation,
//         SyntaxNode node,
//         INamedTypeSymbol symbol,
//         AttributeData attribute,
//         AnalyzerConfigOptions options
//     )
//     {
//         var viewModelTypeSymbol = compilation.GetTypeByMetadataName("Rake.ViewModels.ViewModel");
//
//         var builder = CodeBuilder.Create(symbol).AddNamespaceImport("System");
//
//         return (builder.Build(), null)!;
//     }
//
//     private static void GetViewModelsFromAssembly(
//         INamespaceSymbol namespaceSymbol,
//         List<INamedTypeSymbol> viewModels
//     )
//     {
//         foreach (var member in namespaceSymbol.GetMembers())
//         {
//             switch (member)
//             {
//                 case INamedTypeSymbol namedType:
//                 {
//                     if (
//                         namedType.Name.EndsWith(ViewModelSuffix)
//                         && namedType
//                             is { IsAbstract: false, DeclaredAccessibility: Accessibility.Public }
//                     )
//                     {
//                         viewModels.Add(namedType);
//                     }
//
//                     // Recursively process nested types
//                     foreach (var nestedType in namedType.GetTypeMembers())
//                     {
//                         if (
//                             nestedType.Name.EndsWith(ViewModelSuffix)
//                             && nestedType
//                                 is {
//                                     IsAbstract: false,
//                                     DeclaredAccessibility: Accessibility.Public
//                                 }
//                         )
//                         {
//                             viewModels.Add(nestedType);
//                         }
//                     }
//
//                     break;
//                 }
//                 case INamespaceSymbol nestedNamespace:
//                     GetViewModelsFromAssembly(nestedNamespace, viewModels);
//                     break;
//             }
//         }
//     }
// }
