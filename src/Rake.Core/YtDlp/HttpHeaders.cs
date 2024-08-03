using System.Text.Json.Serialization;

namespace Rake.Core.YtDlp;

public record HttpHeaders(
    [property: JsonPropertyName("User-Agent")] string UserAgent,
    [property: JsonPropertyName("Accept")] string Accept,
    [property: JsonPropertyName("Accept-Language")] string AcceptLanguage,
    [property: JsonPropertyName("Sec-Fetch-Mode")] string SecFetchMode
);
