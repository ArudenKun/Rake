using Humanizer;

namespace Rake.Core.Twitch.Videos;

public record TwitchVideoQuality(
    string Name,
    string Resolution,
    int? Fps,
    IReadOnlyList<string> Codecs,
    long Bitrate,
    ByteSize FileSize
);
