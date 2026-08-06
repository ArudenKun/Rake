namespace Rake.Core.YtDlp;

/// <summary>
/// Fluent configuration methods for Ytdlp.
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class YtDlp
{
    // ==================================================================================================================
    // VERBOSITY AND SIMULATION OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Activate quiet mode. If used with --verbose, print the log to stderr
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithQuiet() => AddFlag("--quiet");

    /// <summary>
    /// Ignore warnings
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoWarnings() => AddFlag("--no-warnings");

    /// <summary>
    /// Do not download the video and do not write anything to disk
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSimulate() => AddFlag("--simulate");

    /// <summary>
    /// Download the video even if printing/listing options are used
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoSimulate() => AddFlag("--no-simulate");

    /// <summary>
    /// Do not download the video but write all related files (Alias: --no-download)
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSkipDownload() => AddFlag("--skip-download");

    /// <summary>
    /// Print various debugging information
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithVerbose() => AddFlag("--verbose");
}
