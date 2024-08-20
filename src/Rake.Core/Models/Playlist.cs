using System.Text.Json.Serialization;

namespace Rake.Core.Models;

public record Playlist(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("_type")] string Type,
    [property: JsonPropertyName("entries")] IReadOnlyList<Entry> Entries,
    [property: JsonPropertyName("webpage_url")] string WebpageUrl,
    [property: JsonPropertyName("original_url")] string OriginalUrl,
    [property: JsonPropertyName("webpage_url_basename")] string WebpageUrlBasename,
    [property: JsonPropertyName("webpage_url_domain")] string WebpageUrlDomain,
    [property: JsonPropertyName("extractor")] string Extractor,
    [property: JsonPropertyName("extractor_key")] string ExtractorKey,
    [property: JsonPropertyName("release_year")] object ReleaseYear,
    [property: JsonPropertyName("playlist_count")] int PlaylistCount,
    [property: JsonPropertyName("epoch")] int Epoch,
    [property: JsonPropertyName("_version")] Version Version
);
