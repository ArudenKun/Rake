namespace Rake.Core.Twitch.Clips;

public record TwitchClip(
    string Title,
    long DurationSeconds,
    string ThumbnailUrl,
    TwitchOwner Broadcaster,
    TwitchGame Game,
    int Views,
    DateTime CreatedAt,
    IReadOnlyList<TwitchClipQuality> Qualities,
    long OffsetSeconds
);
