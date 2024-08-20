using System.Text.Json.Serialization;

namespace Rake.Core.Models;

public record Rechat(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("ext")] string Ext
);
