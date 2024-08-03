using System.Text.Json.Serialization;

namespace Rake.Core.YtDlp;

public record Thumbnail(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("preference")] int? Preference
);
