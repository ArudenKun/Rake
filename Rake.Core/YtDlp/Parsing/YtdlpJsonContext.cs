using System.Text.Json.Serialization;
using Rake.Core.YtDlp.Models;

namespace Rake.Core.YtDlp.Parsing;

/// <summary>
/// Defines the source-generated JSON serialization context for <see cref="Builder.Ytdlp"/>.
/// </summary>
/// <remarks>
/// This class enables high-performance, reflection-free JSON serialization/deserialization.
/// It is essential for:
/// <list type="bullet">
/// <item>Reducing CPU overhead and memory allocations during metadata probing.</item>
/// <item>Supporting Native AOT compilation by providing compile-time type metadata.</item>
/// </list>
/// </remarks>
[JsonSerializable(typeof(Metadata))]
public partial class YtdlpJsonContext : JsonSerializerContext { }
