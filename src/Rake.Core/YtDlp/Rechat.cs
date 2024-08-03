using System.Text.Json.Serialization;

namespace Rake.Core.YtDlp;

public record Rechat(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("ext")] string Ext
);
