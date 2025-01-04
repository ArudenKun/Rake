using System.Text.Json.Serialization;

namespace Rake.Core.Downloading.Internals;

[JsonSerializable(typeof(Metadata))]
[JsonSourceGenerationOptions(
#if DEBUG
    WriteIndented = true,
#endif
    NumberHandling = JsonNumberHandling.AllowReadingFromString
        | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    AllowTrailingCommas = true
)]
internal partial class MetadataJsonContext : JsonSerializerContext;
