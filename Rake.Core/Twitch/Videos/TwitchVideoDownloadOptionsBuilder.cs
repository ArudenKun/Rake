namespace Rake.Core.Twitch.Videos;

internal sealed class TwitchVideoDownloadOptions
{
    public required TwitchVideoQuality Quality { get; init; }
    public TimeSpan? TrimBeginningTime { get; init; }
    public TimeSpan? TrimEndingTime { get; init; }
    public int Threads { get; init; }
    public string? OAuth { get; init; }
    public TrimMode TrimMode { get; init; }

    // Callbacks
    public Action? DownloadStarted { get; init; }
    public Action<DownloadProgressArgs>? DownloadProgress { get; init; }
    public Action? DownloadCompleted { get; init; }

    public Action? VerifyStarted { get; init; }
    public Action<double>? VerifyProgress { get; init; }
    public Action? VerifyCompleted { get; init; }

    public Action? FinalizingStarted { get; init; }
    public Action<double>? FinalizingProgress { get; init; }
    public Action? FinalizingCompleted { get; init; }
}

public sealed class TwitchVideoDownloadOptionsBuilder
{
    private TwitchVideoQuality? _quality;
    private TimeSpan? _trimBeginningTime;
    private TimeSpan? _trimEndingTime;
    private int _threads = 4;
    private string? _oAuth;
    private TrimMode _trimMode = TrimMode.Exact;

    // Callback fields
    private Action? _downloadStarted;
    private Action<DownloadProgressArgs>? _downloadProgress;
    private Action? _downloadCompleted;

    private Action? _verifyStarted;
    private Action<double>? _verifyProgress;
    private Action? _verifyCompleted;

    private Action? _finalizingStarted;
    private Action<double>? _finalizingProgress;
    private Action? _finalizingCompleted;

    internal TwitchVideoDownloadOptionsBuilder() { }

    public TwitchVideoDownloadOptionsBuilder WithQuality(TwitchVideoQuality quality)
    {
        _quality = quality;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder WithTrimBeginning(TimeSpan trimBeginning)
    {
        _trimBeginningTime = trimBeginning;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder WithTrimEnding(TimeSpan trimEnding)
    {
        _trimEndingTime = trimEnding;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder WithThreads(int threads)
    {
        _threads = threads;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder WithOAuth(string oAuth)
    {
        _oAuth = oAuth;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder WithTrimMode(TrimMode trimMode)
    {
        _trimMode = trimMode;
        return this;
    }

    // --- Callback Builder Methods ---

    public TwitchVideoDownloadOptionsBuilder OnDownloadStarted(Action action)
    {
        _downloadStarted = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnDownloadProgress(Action<DownloadProgressArgs> action)
    {
        _downloadProgress = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnDownloadCompleted(Action action)
    {
        _downloadCompleted = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnVerifyStarted(Action action)
    {
        _verifyStarted = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnVerifyProgress(Action<double> action)
    {
        _verifyProgress = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnVerifyCompleted(Action action)
    {
        _verifyCompleted = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnFinalizingStarted(Action action)
    {
        _finalizingStarted = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnFinalizingProgress(Action<double> action)
    {
        _finalizingProgress = action;
        return this;
    }

    public TwitchVideoDownloadOptionsBuilder OnFinalizingCompleted(Action action)
    {
        _finalizingCompleted = action;
        return this;
    }

    internal TwitchVideoDownloadOptions Build()
    {
        if (_quality is null)
            throw new ArgumentNullException(nameof(_quality), "Quality is required");

        return new TwitchVideoDownloadOptions
        {
            Quality = _quality,
            TrimBeginningTime = _trimBeginningTime,
            TrimEndingTime = _trimEndingTime,
            Threads = _threads,
            OAuth = _oAuth,
            TrimMode = _trimMode,
            DownloadStarted = _downloadStarted,
            DownloadProgress = _downloadProgress,
            DownloadCompleted = _downloadCompleted,
            VerifyStarted = _verifyStarted,
            VerifyProgress = _verifyProgress,
            VerifyCompleted = _verifyCompleted,
            FinalizingStarted = _finalizingStarted,
            FinalizingProgress = _finalizingProgress,
            FinalizingCompleted = _finalizingCompleted,
        };
    }
}
