using System.Net;
using System.Text.RegularExpressions;
using PowerKit.Extensions;

namespace Rake.Core.Twitch;

public readonly partial struct TwitchId(string value) : IEquatable<TwitchId>
{
    /// <summary>
    /// Raw ID value.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Indicates whether this ID refers to a Twitch VOD (Video).
    /// </summary>
    public bool IsVideo => ValidateIsVideo(Value);

    /// <summary>
    /// Indicates whether this ID refers to a Twitch Clip.
    /// </summary>
    public bool IsClip => ValidateIsClip(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    /// Checks if the specified string is a Twitch VOD ID or URL.
    /// </summary>
    public static bool ValidateIsVideo(string? videoIdOrUrl)
    {
        if (string.IsNullOrWhiteSpace(videoIdOrUrl))
            return false;

        if (VideoUrlRegex().IsMatch(videoIdOrUrl))
            return true;

        var normalized = TryNormalize(videoIdOrUrl);
        return normalized != null && normalized.All(char.IsDigit);
    }

    /// <summary>
    /// Checks if the specified string is a Twitch Clip ID or URL.
    /// </summary>
    public static bool ValidateIsClip(string? videoIdOrUrl)
    {
        if (string.IsNullOrWhiteSpace(videoIdOrUrl))
            return false;

        if (
            ShortClipUrlRegex().IsMatch(videoIdOrUrl) || ChannelClipUrlRegex().IsMatch(videoIdOrUrl)
        )
            return true;

        var normalized = TryNormalize(videoIdOrUrl);
        return normalized != null && !normalized.All(char.IsDigit);
    }

    /// <summary>
    /// Validates numeric VOD IDs (e.g., 123456789) or alphanumeric Clip IDs/slugs (e.g., EnergeticSnappyPanda-abc123_XYZ).
    /// </summary>
    private static bool IsValid(string videoId) =>
        videoId.Length >= 5 && videoId.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');

    private static string? TryNormalize(string? videoIdOrUrl)
    {
        if (string.IsNullOrWhiteSpace(videoIdOrUrl))
            return null;

        // Check if already passed a valid ID or slug
        // e.g., "1234567890" or "EnergeticSnappyPanda-abc123"
        if (IsValid(videoIdOrUrl))
            return videoIdOrUrl;

        // Try to extract the ID/slug from Twitch URLs
        return
            // VOD URL
            // https://www.twitch.tv/videos/1234567890
            TryExtractId(videoIdOrUrl, VideoUrlRegex())
            // Short Clip URL
            // https://clips.twitch.tv/EnergeticSnappyPanda-abc123
            ?? TryExtractId(videoIdOrUrl, ShortClipUrlRegex())
            // Channel Clip URL
            // https://www.twitch.tv/username/clip/EnergeticSnappyPanda-abc123
            ?? TryExtractId(videoIdOrUrl, ChannelClipUrlRegex());

        static string? TryExtractId(string url, Regex regex)
        {
            var id = regex.Match(url).Groups[1].Value.Pipe(WebUtility.UrlDecode);
            return !string.IsNullOrWhiteSpace(id) && IsValid(id) ? id : null;
        }
    }

    [GeneratedRegex(@"twitch\.tv/videos/(\d+)")]
    private static partial Regex VideoUrlRegex();

    [GeneratedRegex(@"clips\.twitch\.tv/([A-Za-z0-9_-]+)")]
    private static partial Regex ShortClipUrlRegex();

    [GeneratedRegex(@"twitch\.tv/[^/]+/clip/([A-Za-z0-9_-]+)")]
    private static partial Regex ChannelClipUrlRegex();

    /// <summary>
    /// Attempts to parse the specified string as a Twitch VOD/Clip ID or URL.
    /// Returns <see langword="null" /> in case of failure.
    /// </summary>
    public static TwitchId? TryParse(string? videoIdOrUrl) =>
        TryNormalize(videoIdOrUrl)?.Pipe(id => new TwitchId(id));

    /// <summary>
    /// Parses the specified string as a Twitch VOD/Clip ID or URL.
    /// Throws an exception in case of failure.
    /// </summary>
    public static TwitchId Parse(string videoIdOrUrl) =>
        TryParse(videoIdOrUrl)
        ?? throw new ArgumentException($"Invalid Twitch Id or URL '{videoIdOrUrl}'.");

    /// <summary>
    /// Converts string to ID.
    /// </summary>
    public static implicit operator TwitchId(string videoIdOrUrl) => Parse(videoIdOrUrl);

    /// <summary>
    /// Converts ID to string.
    /// </summary>
    public static implicit operator string(TwitchId id) => id.ToString();

    /// <inheritdoc />
    public bool Equals(TwitchId other) =>
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TwitchId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Equality check.
    /// </summary>
    public static bool operator ==(TwitchId left, TwitchId right) => left.Equals(right);

    /// <inheritdoc cref="operator ==(TwitchId, TwitchId)" />
    public static bool operator !=(TwitchId left, TwitchId right) => !(left == right);
}
