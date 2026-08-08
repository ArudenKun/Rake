using System.Net;
using System.Text.RegularExpressions;
using PowerKit.Extensions;

namespace Rake.Core.Twitch;

public readonly struct TwitchId(string value) : IEquatable<TwitchId>
{
    /// <summary>
    /// Raw ID value.
    /// </summary>
    public string Value { get; } = value;

    /// <inheritdoc />
    public override string ToString() => Value;

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
            TryExtractId(videoIdOrUrl, @"twitch\.tv/videos/(\d+)")
            // Short Clip URL
            // https://clips.twitch.tv/EnergeticSnappyPanda-abc123
            ?? TryExtractId(videoIdOrUrl, @"clips\.twitch\.tv/([A-Za-z0-9_-]+)")
            // Channel Clip URL
            // https://www.twitch.tv/username/clip/EnergeticSnappyPanda-abc123
            ?? TryExtractId(videoIdOrUrl, @"twitch\.tv/[^/]+/clip/([A-Za-z0-9_-]+)");

        static string? TryExtractId(string url, string pattern)
        {
            var id = Regex.Match(url, pattern).Groups[1].Value.Pipe(WebUtility.UrlDecode);
            return !string.IsNullOrWhiteSpace(id) && IsValid(id) ? id : null;
        }
    }

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
