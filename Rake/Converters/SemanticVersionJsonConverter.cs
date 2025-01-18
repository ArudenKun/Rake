using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Versioning;

namespace Rake.Converters;

public class SemanticVersionJsonConverter : JsonConverter<SemanticVersion>
{
    public override SemanticVersion? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var versionString = reader.GetString();

        if (string.IsNullOrEmpty(versionString))
            return null;

        return SemanticVersion.TryParse(versionString, out var version) ? version : null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        SemanticVersion value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value.ToString());
}
