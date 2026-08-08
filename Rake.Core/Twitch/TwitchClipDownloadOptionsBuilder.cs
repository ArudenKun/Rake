namespace Rake.Core.Twitch;

internal sealed class TwitchClipDownloadOptions
{
    public string? Quality { get; init; }
    public bool EncodeMetadata { get; init; }

    // Callbacks
    public Action? DownloadStarted { get; init; }
    public Action<double>? DownloadProgress { get; init; }
    public Action? DownloadCompleted { get; init; }

    public Action? EncodingStarted { get; init; }
    public Action<double>? EncodingProgress { get; init; }
    public Action? EncodingCompleted { get; init; }
}

public sealed class TwitchClipDownloadOptionsBuilder
{
    private string? _quality;
    private bool _encodeMetadata = true;

    // Callback fields
    private Action? _downloadStarted;
    private Action<double>? _downloadProgress;
    private Action? _downloadCompleted;

    private Action? _encodingStarted;
    private Action<double>? _encodingProgress;
    private Action? _encodingCompleted;

    internal TwitchClipDownloadOptionsBuilder() { }

    public TwitchClipDownloadOptionsBuilder WithQuality(string quality)
    {
        _quality = quality;
        return this;
    }

    public TwitchClipDownloadOptionsBuilder WithEncodeMetadata(bool encodeMetadata = true)
    {
        _encodeMetadata = encodeMetadata;
        return this;
    }

    // --- Callback Builder Methods ---

    public TwitchClipDownloadOptionsBuilder OnDownloadStarted(Action action)
    {
        _downloadStarted = action;
        return this;
    }

    public TwitchClipDownloadOptionsBuilder OnDownloadProgress(Action<double> action)
    {
        _downloadProgress = action;
        return this;
    }

    public TwitchClipDownloadOptionsBuilder OnDownloadCompleted(Action action)
    {
        _downloadCompleted = action;
        return this;
    }

    public TwitchClipDownloadOptionsBuilder OnEncodingStarted(Action action)
    {
        _encodingStarted = action;
        return this;
    }

    public TwitchClipDownloadOptionsBuilder OnEncodingProgress(Action<double> action)
    {
        _encodingProgress = action;
        return this;
    }

    public TwitchClipDownloadOptionsBuilder OnEncodingCompleted(Action action)
    {
        _encodingCompleted = action;
        return this;
    }

    internal TwitchClipDownloadOptions Build() =>
        new()
        {
            Quality = _quality,
            EncodeMetadata = _encodeMetadata,
            DownloadStarted = _downloadStarted,
            DownloadProgress = _downloadProgress,
            DownloadCompleted = _downloadCompleted,
            EncodingStarted = _encodingStarted,
            EncodingProgress = _encodingProgress,
            EncodingCompleted = _encodingCompleted,
        };
}
