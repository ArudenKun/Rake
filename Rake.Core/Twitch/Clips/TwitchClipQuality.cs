using Humanizer;

namespace Rake.Core.Twitch.Clips;

public record TwitchClipQuality(
    string Name,
    int Width,
    int Height,
    int? Fps,
    IReadOnlyList<string> Codecs,
    long Bitrate,
    ByteSize FileSize
);
