using System.Text.Json.Serialization;

namespace Rake.Core.YtDlp;

public record Fragment(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("duration")] int Duration
);
