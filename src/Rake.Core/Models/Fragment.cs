using System.Text.Json.Serialization;

namespace Rake.Core.Models;

public record Fragment(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("duration")] int Duration
);
