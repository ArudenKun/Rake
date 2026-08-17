// using System.Collections.Immutable;
// using Rake.SourceGenerators.Extensions;
// using GenerationContext = Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext;
//
// namespace Rake.SourceGenerators.Abstractions;
//
// internal abstract class SourceGeneratorForDeclaredAssemblyWithAttribute<TAttribute>
//     : SourceGeneratorForDeclaredAssemblyWithAttribute
//     where TAttribute : Attribute
// {
//     protected override string AttributeType => typeof(TAttribute).Name;
// }
//
// internal abstract class SourceGeneratorForDeclaredAssemblyWithAttribute : IIncrementalGenerator
// {
//     private readonly string _attributeType;
//     private readonly string _attributeName;
//
//     protected SourceGeneratorForDeclaredAssemblyWithAttribute()
//     {
//         // ReSharper disable once VirtualMemberCallInConstructor
//         _attributeType = AttributeType.AddSuffix("Attribute");
//         _attributeName = _attributeType.TrimSuffix("Attribute");
//     }
//
//     protected abstract string AttributeType { get; }
//     protected virtual IEnumerable<(string Name, string Source)> StaticSources => [];
//
//     public void Initialize(GenerationContext context)
//     {
//         foreach (var (name, source) in StaticSources)
//             context.RegisterPostInitializationOutput(x => x.AddSource($"{name}.g.cs", source));
//
//         var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
//             IsSyntaxTarget,
//             GetSyntaxTarget
//         );
//
//         var compilationProvider = context
//             .CompilationProvider.Combine(syntaxProvider.Collect())
//             .Combine(context.AnalyzerConfigOptionsProvider);
//
//         context.RegisterImplementationSourceOutput(
//             compilationProvider,
//             (spc, provider) =>
//                 OnExecute(spc, provider.Left.Left, provider.Left.Right, provider.Right)
//         );
//
//         bool IsSyntaxTarget(SyntaxNode node, CancellationToken _)
//         {
//             // Assembly attributes are applied via AttributeListSyntax at the root level with an 'assembly' specifier
//             if (
//                 node is AttributeListSyntax attributeList
//                 && attributeList.Target?.Identifier.ValueText == "assembly"
//             )
//             {
//                 foreach (var attribute in attributeList.Attributes)
//                 {
//                     if (attribute.Name.ToString() == _attributeName)
//                         return true;
//                 }
//             }
//
//             return false;
//         }
//
//         static AttributeListSyntax GetSyntaxTarget(
//             GeneratorSyntaxContext context,
//             CancellationToken _
//         ) => (AttributeListSyntax)context.Node;
//
//         void OnExecute(
//             SourceProductionContext spc,
//             Compilation compilation,
//             ImmutableArray<AttributeListSyntax> attributeLists,
//             AnalyzerConfigOptionsProvider options
//         )
//         {
//             try
//             {
//                 if (attributeLists.IsEmpty)
//                     return;
//
//                 var assemblySymbol = compilation.Assembly;
//
//                 // Find the matching AttributeData on the assembly symbol
//                 var attributeData = assemblySymbol
//                     .GetAttributes()
//                     .FirstOrDefault(x => x.AttributeClass?.Name == _attributeType);
//
//                 if (attributeData is null)
//                     return;
//
//                 // Grab the syntax node for diagnostic location anchoring
//                 var syntaxNode = attributeLists.First();
//
//                 var (generatedCode, error) = _GenerateCode(
//                     compilation,
//                     syntaxNode,
//                     assemblySymbol,
//                     attributeData,
//                     options.GlobalOptions
//                 );
//
//                 if (generatedCode is null)
//                 {
//                     var descriptor = new DiagnosticDescriptor(
//                         error.Id.IfNullOrWhiteSpace(_attributeName),
//                         error.Title,
//                         error.Message,
//                         error.Category.IfNullOrWhiteSpace("Usage"),
//                         DiagnosticSeverity.Error,
//                         true
//                     );
//                     var diagnostic = Diagnostic.Create(
//                         descriptor,
//                         attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation()
//                             ?? syntaxNode.GetLocation()
//                     );
//                     spc.ReportDiagnostic(diagnostic);
//                     return;
//                 }
//
//                 spc.AddSource(GenerateFilename(assemblySymbol), generatedCode);
//             }
//             catch (Exception e)
//             {
//                 _ = e;
//                 throw;
//             }
//         }
//     }
//
//     protected abstract (string GeneratedCode, DiagnosticDetail Error) GenerateCode(
//         Compilation compilation,
//         SyntaxNode node,
//         IAssemblySymbol symbol,
//         AttributeData attribute,
//         AnalyzerConfigOptions options
//     );
//
//     private (string? GeneratedCode, DiagnosticDetail Error) _GenerateCode(
//         Compilation compilation,
//         SyntaxNode node,
//         IAssemblySymbol symbol,
//         AttributeData attribute,
//         AnalyzerConfigOptions options
//     )
//     {
//         try
//         {
//             return GenerateCode(compilation, node, symbol, attribute, options);
//         }
//         catch (Exception e)
//         {
//             return (null, InternalError(e));
//         }
//
//         static DiagnosticDetail InternalError(Exception e) => new("Internal Error", e.Message)
//         {
//
//         }
//     }
//
//     private const string Ext = ".g.cs";
//     private const int MaxFileLength = 255;
//
//     protected virtual string GenerateFilename(IAssemblySymbol symbol)
//     {
//         var gn = $"{Format(symbol)}{Ext}";
//         return gn;
//
//         static string Format(ISymbol symbol) =>
//             string.Join("_", $"{symbol.Name}".Split(InvalidFileNameChars))
//                 .Truncate(MaxFileLength - Ext.Length);
//     }
//
//     private static readonly char[] InvalidFileNameChars =
//     [
//         '\"',
//         '<',
//         '>',
//         '|',
//         '\0',
//         (char)1,
//         (char)2,
//         (char)3,
//         (char)4,
//         (char)5,
//         (char)6,
//         (char)7,
//         (char)8,
//         (char)9,
//         (char)10,
//         (char)11,
//         (char)12,
//         (char)13,
//         (char)14,
//         (char)15,
//         (char)16,
//         (char)17,
//         (char)18,
//         (char)19,
//         (char)20,
//         (char)21,
//         (char)22,
//         (char)23,
//         (char)24,
//         (char)25,
//         (char)26,
//         (char)27,
//         (char)28,
//         (char)29,
//         (char)30,
//         (char)31,
//         ':',
//         '*',
//         '?',
//         '\\',
//         '/',
//     ];
// }
