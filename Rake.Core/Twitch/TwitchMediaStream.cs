using Humanizer;

namespace Rake.Core.Twitch;

public record TwitchVideoStream(
    string Name,
    string Resolution,
    int? Fps,
    IReadOnlyList<string> Codecs,
    long Bitrate,
    ByteSize FileSize
);
