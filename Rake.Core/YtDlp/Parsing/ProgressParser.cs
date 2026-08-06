using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rake.Core.YtDlp.Events;

namespace Rake.Core.YtDlp.Parsing;

/// <summary>
/// Parses yt-dlp output and raises strongly-typed progress,
/// download, and post-processing events.
/// </summary>
public sealed class ProgressParser
{
    private readonly Dictionary<Regex, Action<Match>> _regexHandlers;
    private readonly ILogger _logger;

    // State tracking
    private bool _isDownloadCompleted;
    private bool _postProcessingStarted;
    private int _postProcessStepCount;

    // ───────────── Events (unchanged) ─────────────
    #region Events
    internal event EventHandler<DownloadProgressEventArgs>? ProgressDownload;
    internal event EventHandler<string>? ProgressMessage;
    internal event EventHandler<string>? DownloadCompleted;
    internal event EventHandler<string>? PostProcessingStarted;
    internal event EventHandler<string>? PostProcessingCompleted;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressParser"/> class.
    /// </summary>
    /// <param name="logger">
    /// Optional logger used for diagnostic and parsing messages.
    /// If <see langword="null"/>, a default logger implementation is used.
    /// </param>
    public ProgressParser(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;

        _regexHandlers = new Dictionary<Regex, Action<Match>>
        {
            // Beginning
            { RegexPatterns.DownloadDestination, HandleDownloadDestination },
            { RegexPatterns.ResumeDownload, HandleResumeDownload },
            { RegexPatterns.DownloadAlreadyDownloaded, HandleDownloadAlreadyCompleted },
            // Progress
            { RegexPatterns.DownloadProgressComplete, HandleDownloadProgressComplete },
            { RegexPatterns.DownloadProgressWithFrag, HandleDownloadProgressWithFrag },
            { RegexPatterns.DownloadProgress, HandleDownloadProgress },
            { RegexPatterns.DownloadProgressWithAria2, HandleDownloadProgressWithAria2 },
            { RegexPatterns.DownloadProgressWithAria2Next, HandleDownloadProgressWithAria2 },
            // Errors & Subtitles/Playlist
            { RegexPatterns.UnknownError, HandleUnknownError },
            { RegexPatterns.SpecificError, HandleSpecificError },
            {
                RegexPatterns.PlaylistItem,
                m =>
                    LogAndNotify(
                        LogLevel.Information,
                        $"Playlist progress: item {m.Groups["item"].Value}/{m.Groups["total"].Value} - {m.Groups["playlist"].Value}"
                    )
            },
            // Post-processing
            { RegexPatterns.FixupM3u8, HandlePostProcessingStep },
            { RegexPatterns.VideoRemuxer, HandlePostProcessingStep },
            { RegexPatterns.Metadata, HandlePostProcessingStep },
            { RegexPatterns.ThumbnailsConvertor, HandlePostProcessingStep },
            { RegexPatterns.EmbedThumbnail, HandlePostProcessingStep },
            { RegexPatterns.MoveFiles, HandlePostProcessingStep },
            { RegexPatterns.PostProcessorGeneric, HandlePostProcessingStep },
            { RegexPatterns.DeleteingOriginalFile, HandlePostProcessingStep },
        };
    }

    /// <summary>
    /// Parses a single line of yt-dlp output and dispatches it to the
    /// appropriate registered handler if a known pattern is matched.
    /// </summary>
    /// <param name="output">Raw output line from the yt-dlp process.</param>
    public void ParseProgress(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return;

        foreach (var (regex, handler) in _regexHandlers)
        {
            var match = regex.Match(output);
            if (match.Success)
            {
                handler(match);
                return;
            }
        }

        HandleUnknownOutput(output);
    }

    /// <summary>
    /// Resets the internal state of the progress parser.
    /// </summary>
    /// <remarks>
    /// Clears all tracking flags related to download and post-processing stages.
    /// This does not clear registered regex handlers.
    /// </remarks>
    public void Reset()
    {
        _isDownloadCompleted = false;
        _postProcessingStarted = false;
        _postProcessStepCount = 0;
        _logger.Log(LogLevel.Information, "Progress parser state reset.");
    }

    // ───────────── Event Handlers (existing + improved) ─────────────
    #region Event Handlers

    private void HandleDownloadDestination(Match match)
    {
        string path = match.Groups["path"].Value;
        LogAndNotify(LogLevel.Information, $"Download destination: {path}");
    }

    private void HandleResumeDownload(Match match)
    {
        string bytePosition = match.Groups["byte"].Value;
        var message = $"Resuming download at byte {bytePosition}";
        LogAndNotify(LogLevel.Information, message);
        ProgressDownload?.Invoke(this, new DownloadProgressEventArgs { Message = message });
    }

    private void HandleDownloadProgress(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["total"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;

        var isParsed = double.TryParse(percentString.Replace("%", ""), out double percent);

        if (!isParsed)
            percent = 0;
        if (!isParsed || percent >= 100)
            return;

        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Message = $"Progress: {percent:F2}% | {sizeString} | {speedString} | ETA {etaString}",
        };

        LogAndNotify(LogLevel.Information, args.Message);
        ProgressDownload?.Invoke(this, args);

        if (percent >= 99.0 && !_isDownloadCompleted)
        {
            HandleDownloadProgressComplete(match);
        }
    }

    private void HandleDownloadProgressWithAria2(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string downloadedString = match.Groups["downloaded"].Value;
        string totalSizeString = match.Groups["total"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;
        string connectionsString = match.Groups["connections"].Value;

        var isParsed = double.TryParse(percentString.Replace("%", ""), out var percent);

        if (!isParsed)
            percent = 0;
        if (!isParsed || percent >= 100)
            return;

        // Append "/s" to speed if missing to match standard yt-dlp format
        string formattedSpeed = speedString.EndsWith("/s", StringComparison.OrdinalIgnoreCase)
            ? speedString
            : $"{speedString}/s";

        string sizeDisplay = $"{downloadedString}/{totalSizeString}";

        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeDisplay,
            Speed = formattedSpeed,
            ETA = etaString,
            Message =
                $"Progress: {percent:F2}% | {sizeDisplay} | {formattedSpeed} | ETA {etaString} | CN:{connectionsString}",
        };

        LogAndNotify(LogLevel.Information, args.Message);
        ProgressDownload?.Invoke(this, args);

        if (percent >= 99.0 && !_isDownloadCompleted)
        {
            HandleDownloadProgressComplete(match);
        }
    }

    private void HandleDownloadProgressWithFrag(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;
        string fragString = match.Groups["frag"].Value;

        var isParsed = double.TryParse(percentString.Replace("%", ""), out double percent);

        if (!isParsed)
            percent = 0;
        if (!isParsed || percent >= 100)
            return;

        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Fragments = fragString,
            Message =
                $"Progress: {percent:F2}% | {sizeString} | {speedString} | ETA {etaString} | {fragString}",
        };

        LogAndNotify(LogLevel.Information, args.Message);
        ProgressDownload?.Invoke(this, args);

        // Only trigger complete if really done (avoid false 100% on fragment level)
        if (percent >= 99.0 && IsFinalFragment(fragString) && !_isDownloadCompleted)
        {
            HandleDownloadProgressComplete(match);
        }
    }

    private static bool IsFinalFragment(string frag)
    {
        if (string.IsNullOrEmpty(frag) || !frag.Contains('/'))
            return true;

        var parts = frag.Split('/');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out int current)
            && int.TryParse(parts[1], out int total)
            && current >= total - 1; // allow last 1-2 fragments due to concurrency
    }

    private void HandleDownloadProgressComplete(Match match)
    {
        if (_isDownloadCompleted)
            return;

        _isDownloadCompleted = true;

        string percent = match.Groups["percent"]?.Value ?? "100";
        string size = match.Groups["size"]?.Value ?? match.Groups["total"]?.Value ?? "unknown";

        var message = $"Download finished: {percent}% of {size}";

        LogAndNotifyComplete(message);
        _logger.Log(LogLevel.Information, "Download marked as completed.");
    }

    private void HandleDownloadAlreadyCompleted(Match match)
    {
        string path = match.Groups["path"].Value;
        var message = $"Download completed: {path} has already been downloaded.";
        LogAndNotify(LogLevel.Information, message);
        ProgressDownload?.Invoke(this, new DownloadProgressEventArgs { Message = message });
    }

    private void HandleUnknownError(Match match)
    {
        LogAndNotify(LogLevel.Error, $"Unknown error: {match.Value}");
    }

    private void HandleSpecificError(Match match)
    {
        string error = match.Groups["error"].Value;
        LogAndNotify(LogLevel.Error, $"Error: {error}");
    }

    private void HandlePostProcessingStep(Match match)
    {
        // Ensure download is marked completed
        if (!_isDownloadCompleted)
            _isDownloadCompleted = true;

        // Start post-processing phase **only once** per download
        if (!_postProcessingStarted)
        {
            _postProcessingStarted = true;
            _postProcessStepCount = 0;

            LogAndNotify(LogLevel.Information, "Post-processing started");
            PostProcessingStarted?.Invoke(this, "Post-processing started");
        }

        _postProcessStepCount++;

        // Extract processor and action safely
        string processor = match.Groups["processor"].Success
            ? match.Groups["processor"].Value.Trim()
            : "PostProcessor";

        string action = match.Groups["action"].Success
            ? match.Groups["action"].Value.Trim()
            : match.Value.Trim();

        var message = $"[{processor}] {action}";
        LogAndNotify(LogLevel.Information, $"Post-processing [{_postProcessStepCount}]: {message}");

        // Trigger completion when we hit the real last step (MoveFiles is usually the final one)
        bool isFinalStep =
            processor.Equals("MoveFiles", StringComparison.OrdinalIgnoreCase)
            || action.Contains("Moving file", StringComparison.OrdinalIgnoreCase)
            || _postProcessStepCount >= 10; // safety net for unusual cases

        if (isFinalStep)
        {
            var completeMsg = "Post-processing completed successfully";

            LogAndNotify(LogLevel.Information, completeMsg);
            PostProcessingCompleted?.Invoke(this, completeMsg);

            _logger.Log(LogLevel.Information, "OnPostProcessingComplete event triggered.");

            // Reset flags so next download starts fresh
            Reset();
        }
    }

    private void HandleUnknownOutput(string output)
    {
        var lower = output.ToLowerInvariant().Trim();

        LogLevel logType =
            lower.Contains("error") ? LogLevel.Error
            : lower.Contains("warning") ? LogLevel.Warning
            : lower.Contains("[debug]") ? LogLevel.Debug
            : LogLevel.Information;

        LogAndNotify(logType, output);
    }

    #endregion

    // ───────────── Helpers (unchanged except minor polish) ─────────────
    #region helpers
    private void LogAndNotify(LogLevel logType, string message)
    {
        _logger.Log(logType, message);
        ProgressMessage?.Invoke(this, message);
    }

    private void LogAndNotifyComplete(string message)
    {
        _logger.Log(LogLevel.Information, message);
        DownloadCompleted?.Invoke(this, message);
    }
    #endregion
}
