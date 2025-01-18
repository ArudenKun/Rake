using System.Text.Json.Serialization;

namespace Rake.Utilities.Downloading;

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
