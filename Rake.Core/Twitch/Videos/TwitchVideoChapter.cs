namespace Rake.Core.Twitch.Videos;

public record TwitchVideoChapter(
    string Category,
    string Type,
    int StartSeconds,
    int EndSeconds,
    int LengthSeconds
);
