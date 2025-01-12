using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Versioning;

namespace Rake.Core.Converters;

public class SemanticVersionHashSetJsonConverter : JsonConverter<HashSet<SemanticVersion>>
{
    public override HashSet<SemanticVersion> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of an array.");
        }

        var versions = new HashSet<SemanticVersion>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected a string, but got {reader.TokenType}.");
            }

            var versionString = reader.GetString();

            if (string.IsNullOrWhiteSpace(versionString))
            {
                throw new JsonException("Version string cannot be null or empty.");
            }

            if (SemanticVersion.TryParse(versionString, out var version))
            {
                versions.Add(version);
            }
            else
            {
                throw new JsonException($"Invalid SemanticVersion: {versionString}");
            }
        }

        return versions;
    }

    public override void Write(
        Utf8JsonWriter writer,
        HashSet<SemanticVersion> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();

        foreach (var version in value)
        {
            writer.WriteStringValue(version.ToString());
        }

        writer.WriteEndArray();
    }
}
