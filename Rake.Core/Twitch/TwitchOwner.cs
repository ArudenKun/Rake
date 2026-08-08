using System.Text.Json.Serialization;

namespace Rake.Core.Twitch;

public record TwitchOwner(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("login")] string Login
);
