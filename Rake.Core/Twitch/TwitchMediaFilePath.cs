using System.Net;
using PowerKit.Extensions;

namespace Rake.Core.Twitch;

public readonly struct TwitchMediaFilePath : IEquatable<TwitchMediaFilePath>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwitchMediaFilePath"/> struct.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the provided path or URL does not end in .mp4 or .m4a</exception>
    public TwitchMediaFilePath(string pathOrUrl)
    {
        Value =
            TryNormalize(pathOrUrl)
            ?? throw new ArgumentException(
                $"Invalid or unsupported media file extension in '{pathOrUrl}'. Only .mp4 and .m4a are allowed.",
                nameof(pathOrUrl)
            );
    }

    /// <summary>
    /// Validated media file path or name.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    /// Validates that the input ends with a .mp4 or .m4a extension.
    /// </summary>
    private static bool IsValid(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".m4a", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryNormalize(string? pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
            return null;

        var cleanPath = pathOrUrl.Trim();

        // Handle URLs by stripping query parameters or fragments prior to validation
        if (
            Uri.TryCreate(cleanPath, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        )
        {
            cleanPath = uri.AbsolutePath.Pipe(WebUtility.UrlDecode);
        }

        return IsValid(cleanPath) ? cleanPath : null;
    }

    /// <summary>
    /// Attempts to parse the specified string or URL as an .mp4 or .m4a file path.
    /// Returns <see langword="null" /> in case of failure.
    /// </summary>
    public static TwitchMediaFilePath? TryParse(string? pathOrUrl) =>
        TryNormalize(pathOrUrl)?.Pipe(path => new TwitchMediaFilePath(path));

    /// <summary>
    /// Parses the specified string or URL as an .mp4 or .m4a file path.
    /// Throws an exception in case of failure.
    /// </summary>
    public static TwitchMediaFilePath Parse(string pathOrUrl) =>
        TryParse(pathOrUrl)
        ?? throw new ArgumentException(
            $"Invalid or unsupported media file extension in '{pathOrUrl}'. Only .mp4 and .m4a are allowed."
        );

    /// <summary>
    /// Converts string to TwitchMediaFilePath.
    /// </summary>
    public static implicit operator TwitchMediaFilePath(string pathOrUrl) => Parse(pathOrUrl);

    /// <summary>
    /// Converts TwitchMediaFilePath to string.
    /// </summary>
    public static implicit operator string(TwitchMediaFilePath file) => file.ToString();

    /// <inheritdoc />
    public bool Equals(TwitchMediaFilePath other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TwitchMediaFilePath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <summary>
    /// Equality check.
    /// </summary>
    public static bool operator ==(TwitchMediaFilePath left, TwitchMediaFilePath right) =>
        left.Equals(right);

    /// <inheritdoc cref="operator ==(TwitchMediaFilePath, TwitchMediaFilePath)" />
    public static bool operator !=(TwitchMediaFilePath left, TwitchMediaFilePath right) =>
        !(left == right);
}
