using System.Text.Json.Serialization;

namespace Rake.Core.Twitch;

public record TwitchGame(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("boxArtURL")] string BoxArtUrl
);
