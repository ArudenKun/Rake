using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using H.Generators.Extensions;
using Humanizer;
using Microsoft.CodeAnalysis;
using Rake.Generator.Models;
using Rake.Generator.Utilities;

namespace Rake.Generator;

[Generator]
internal sealed class AvaloniaResourceGenerator : IIncrementalGenerator
{
    private const string Id = "AG";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context
            .AdditionalTextsProvider.Select((x, _) => new AvaloniaResource(x.Path))
            .Collect()
            .SelectAndReportExceptions(GetSource, context, Id)
            .AddSource(context);
    }

    private static EquatableArray<FileWithName> GetSource(
        ImmutableArray<AvaloniaResource> assets,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        assets = assets.RemoveAll(x => x.Path.EndsWith("axaml"));
        const string modifier = "internal";
        const string className = "AvaloniaResources";

        return new[]
        {
            new FileWithName(
                $"{Meta.Namespace}.AvaloniaResource.g.cs",
                GetAssetCode(Meta.Namespace)
            ),
            new FileWithName(
                $"{Meta.Namespace}.AvaloniaResources.g.cs",
                GetAssetsCode(Meta.Namespace, modifier, className, assets)
            )
        }
            .ToImmutableArray()
            .AsEquatableArray();
    }

    private static string GetAssetCode(string @namespace)
    {
        return $$"""
            using System;
            using System.IO;
            using System.Linq;
            using System.Reflection;
            using Avalonia.Platform;

            #nullable enable

            namespace {{@namespace}}
            {
                internal class AvaloniaResource
                {
                    public string FileName { get; }

                    public AvaloniaResource(string fileName)
                    {
                        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
                    }

                    /// <summary>
                    /// Searches for a file among Embedded resources <br/>
                    /// Throws an <see cref="ArgumentException"/> if nothing is found or more than one match is found <br/>
                    /// </summary>
                    /// <param name="assembly"></param>
                    /// <exception cref="ArgumentNullException"></exception>
                    /// <exception cref="ArgumentException"></exception>
                    /// <returns></returns>
                    public Stream AsStream(Assembly? assembly = null)
                    {
                        assembly ??= Assembly.GetExecutingAssembly();

                        try
                        {
                            var assemblyName = AssemblyName.GetAssemblyName(assembly.Location).Name;
                            var assets = AssetLoader.GetAssets(new Uri($"avares://{assemblyName}"), null);

                            var asset = assets.Select(x => x)
                                .Single(x => x.ToString().EndsWith(FileName, StringComparison.InvariantCultureIgnoreCase));

                            if (AssetLoader.Exists(asset))
                            {
                                return AssetLoader.Open(asset);
                            }

                            throw new ArgumentException($"\"{FileName}\" is not found in avalonia resources");
                        }
                        catch (InvalidOperationException exception)
                        {
                            throw new ArgumentException(
                                "Not a single one was found or more than one resource with the given name was found. " +
                                "Make sure there are no collisions",
                                exception);
                        }
                    }

                    /// <summary>
                    /// Searches for a file among Embedded resources <br/>
                    /// Throws an <see cref="ArgumentException"/> if nothing is found or more than one match is found <br/>
                    /// </summary>
                    /// <param name="assembly"></param>
                    /// <exception cref="ArgumentNullException"></exception>
                    /// <exception cref="ArgumentException"></exception>
                    /// <returns></returns>
                    public byte[] AsBytes(Assembly? assembly = null)
                    {
                        using var stream = AsStream(assembly);
                        using var memoryStream = new MemoryStream();

                        stream.CopyTo(memoryStream);

                        return memoryStream.ToArray();
                    }

                    /// <summary>
                    /// Searches for a file among Embedded resources <br/>
                    /// Throws an <see cref="ArgumentException"/> if nothing is found or more than one match is found <br/>
                    /// </summary>
                    /// <param name="assembly"></param>
                    /// <exception cref="ArgumentNullException"></exception>
                    /// <exception cref="ArgumentException"></exception>
                    /// <returns></returns>
                    public string AsString(Assembly? assembly = null)
                    {
                        using var stream = AsStream(assembly);
                        using var reader = new StreamReader(stream);

                        return reader.ReadToEnd();
                    }
                    
                    /// <summary>
                    /// Searches for a file among Embedded resources <br/>
                    /// Throws an <see cref="ArgumentException"/> if nothing is found or more than one match is found <br/>
                    /// </summary>
                    /// <param name="assembly"></param>
                    /// <exception cref="ArgumentNullException"></exception>
                    /// <exception cref="ArgumentException"></exception>
                    /// <returns></returns>
                    public string GetPath(Assembly? assembly = null)
                    {
                        assembly ??= Assembly.GetExecutingAssembly();
                       
                        var assemblyName = AssemblyName.GetAssemblyName(assembly.Location).Name;
                        var assets = AssetLoader.GetAssets(new Uri($"avares://{assemblyName}"), null);
                        
                        var asset = assets
                            .Single(x => x.ToString().EndsWith(FileName, StringComparison.InvariantCultureIgnoreCase));
                        
                        if (AssetLoader.Exists(asset))
                        {
                            return asset.ToString();
                        }
                        
                        throw new ArgumentException($"\"{FileName}\" is not found in avalonia resources");
                    }
                }
            }
            """;
    }

    private static string GetAssetsCode(
        string @namespace,
        string modifier,
        string className,
        IReadOnlyCollection<AvaloniaResource> assets
    )
    {
        var properties = assets
            .Select(static resource =>
                (
                    Name: Path.GetFileName(resource.Path)
                        .Replace("-", string.Empty)
                        .Replace(".", "_")
                        .Replace(" ", "_")
                        .Dehumanize(),
                    FileName: Path.GetFileName(resource.Path)
                )
            )
            .ToArray();

        var source = SourceStringBuilder.Create();

        source.NamespaceBlockBrace(
            @namespace,
            () =>
            {
                source.Line($"{modifier} static class {className}");
                source.BlockBrace(() =>
                {
                    foreach (var (name, fileName) in properties)
                    {
                        source.Line(
                            $"public static AvaloniaResource {name} => new AvaloniaResource(\"{fileName}\");"
                        );
                    }
                });
            }
        );

        return source.ToString();
    }
}
