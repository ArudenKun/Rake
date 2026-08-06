using System.Text.RegularExpressions;

namespace Rake.Core.YtDlp.Parsing;

internal static partial class RegexPatterns
{
    // Options constant for readability
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    // ───────────── Core ─────────────
    [GeneratedRegex(@"\[download\]\s*Destination:\s*(?<path>.+)", Options)]
    public static partial Regex DownloadDestination { get; }

    [GeneratedRegex(@"\[download\]\s*Resuming download at byte\s*(?<byte>\d+)", Options)]
    public static partial Regex ResumeDownload { get; }

    [GeneratedRegex(@"\[download\]\s*(?<path>[^\n]+?)\s*has already been downloaded", Options)]
    public static partial Regex DownloadAlreadyDownloaded { get; }

    [GeneratedRegex(
        @"\[download\]\s+(?:(?<percent>[\d\.]+)%(?:\s+of\s+\~?\s*(?<total>[\d\.\w]+))?\s+at\s+(?:(?<speed>[\d\.\w]+\/s)|[\w\s]+)\s+ETA\s(?<eta>[\d\:]+))?",
        Options
    )]
    public static partial Regex DownloadProgress { get; }

    [GeneratedRegex(
        @"\[#(?<id>[a-fA-F0-9]+)\s+(?<downloaded>[\d\.\w]+)\/(?<total>[\d\.\w]+)\((?<percent>\d+)%\)\s+CN:(?<connections>\d+)\s+DL:(?<speed>[\d\.\w]+)\s+ETA:(?<eta>[\d\w]+)\]",
        Options
    )]
    public static partial Regex DownloadProgressWithAria2 { get; }

    [GeneratedRegex(
        @"\[#(?<id>[a-fA-F0-9]+)\s+\[[^\]]*\]\s+(?<percent>\d+)%\s+(?<downloaded>[\d\.\w]+)\/(?<total>[\d\.\w]+)\s+CN:(?<connections>\d+)\s+DL:(?<speed>[\d\.\w]+)\s+ETA:(?<eta>[\d\w]+)\]",
        Options
    )]
    public static partial Regex DownloadProgressWithAria2Next { get; }

    [GeneratedRegex(
        @"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(~?\s*(?<size>[^\s]+))\s*at\s*(?<speed>[^\s]+)\s*ETA\s*(?<eta>[^\s]+)\s*\(frag\s*(?<frag>\d+/\d+)\)",
        Options
    )]
    public static partial Regex DownloadProgressWithFrag { get; }

    [GeneratedRegex(
        @"\[download\]\s*(?<percent>100(?:\.0)?)%\s*of\s*(?<size>[^\s]+)\s*at\s*(?<speed>[^\s]+|Unknown)\s*ETA\s*(?<eta>[^\s]+|Unknown)",
        Options
    )]
    public static partial Regex DownloadProgressComplete { get; }

    [GeneratedRegex(@"\[download\]\s*Unknown error", Options)]
    public static partial Regex UnknownError { get; }

    [GeneratedRegex(@"\[Merger\]\s*Merging formats into\s*""(?<path>.+)""", Options)]
    public static partial Regex MergingFormats { get; }

    [GeneratedRegex(@"Deleting original file\s+(?<path>.+?)\s+\(pass -k to keep\)", Options)]
    public static partial Regex DeleteingOriginalFile { get; }

    [GeneratedRegex(@"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Extracting metadata", Options)]
    public static partial Regex ExtractingMetadata { get; }

    [GeneratedRegex(@"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*ERROR:\s*(?<error>.+)", Options)]
    public static partial Regex SpecificError { get; }

    [GeneratedRegex(@"\[info\]\s*Downloading subtitles:\s*(?<language>[^\s]+)", Options)]
    public static partial Regex DownloadingSubtitles { get; }

    // Basic playlist item progress (when downloading playlists)
    [GeneratedRegex(
        @"\[download\]\s*Downloading playlist:\s*(?<playlist>.+?)\s*;\s*Downloading\s*(?<item>\d+)\s*of\s*(?<total>\d+)",
        Options
    )]
    public static partial Regex PlaylistItem { get; }

    // Warning / debug lines (useful for better unknown classification)
    [GeneratedRegex(@"\[warning\]\s*(?<message>.+)", Options)]
    public static partial Regex WarningLine { get; }

    [GeneratedRegex(@"\[debug\]\s*(?<message>.+)", Options)]
    public static partial Regex DebugLine { get; }

    [GeneratedRegex(@"\[FixupM3u8\]\s*(?<action>.+)", Options)]
    public static partial Regex FixupM3u8 { get; }

    [GeneratedRegex(@"\[VideoRemuxer\]\s*(?<action>.+)", Options)]
    public static partial Regex VideoRemuxer { get; }

    [GeneratedRegex(@"\[Metadata\]\s*(?<action>.+)", Options)]
    public static partial Regex Metadata { get; }

    [GeneratedRegex(@"\[ThumbnailsConvertor\]\s*(?<action>.+)", Options)]
    public static partial Regex ThumbnailsConvertor { get; }

    [GeneratedRegex(@"\[EmbedThumbnail\]\s*(?<action>.+)", Options)]
    public static partial Regex EmbedThumbnail { get; }

    [GeneratedRegex(@"\[MoveFiles\]\s*(?<action>.+)", Options)]
    public static partial Regex MoveFiles { get; }

    // Generic fallback for any unknown post-processor
    [GeneratedRegex(
        @"\[(?<processor>Merger|ModifyChapters|SplitChapters|ExtractAudio|VideoRemuxer|VideoConvertor|Metadata|EmbedSubtitle|EmbedThumbnail|SubtitlesConvertor|ThumbnailsConvertor|FixupStretched|FixupM4a|FixupM3u8|FixupTimestamp|FixupDuration|MoveFiles|ffmpeg|ConvertSubs|SponsorBlock)\]\s*(?<action>.+)",
        Options
    )]
    public static partial Regex PostProcessorGeneric { get; }
}
