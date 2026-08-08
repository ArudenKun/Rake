namespace Rake.Core.Twitch;

public record TwitchMediaChapter(
    string Category,
    string Type,
    int StartSeconds,
    int EndSeconds,
    int LengthSeconds
);
