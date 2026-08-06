namespace Rake.Core.YtDlp;

/// <summary>
/// Fluent configuration methods for Ytdlp.
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class YtDlp
{
    // ==================================================================================================================
    // FILE-SYSTEM OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Sets the home folder for yt-dlp (used for config or as base directory).
    /// Path is automatically normalized and quoted.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public YtDlp WithHomeFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Home folder path required");
        return new YtDlp(this, homeFolder: Path.GetFullPath(path).Replace('\\', '/'));
    }

    /// <summary>
    /// Sets the temporary folder for yt-dlp intermediate files (fragments, etc.).
    /// Path is automatically normalized and quoted.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public YtDlp WithTempFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Temp folder path required");
        return new YtDlp(this, tempFolder: Path.GetFullPath(path).Replace('\\', '/'));
    }

    /// <summary>
    /// Sets the output folder
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public YtDlp WithOutputFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Output folder path required");
        return new YtDlp(this, outputFolder: Path.GetFullPath(path).Replace('\\', '/'));
    }

    /// <summary>
    /// Output filename template
    /// </summary>
    /// <param name="template"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithOutputTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Template required");
        return new YtDlp(this, outputTemplate: template.Trim());
    }

    /// <summary>
    /// Restrict filenames to only ASCII characters, and avoid "AND" and spaces in filenames
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithRestrictFilenames() => AddFlag("--restrict-filenames");

    /// <summary>
    /// Force filenames to be Windows-compatible
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithWindowsFilenames() => AddFlag("--windows-filenames");

    /// <summary>
    /// Limit the filename length (excluding extension) to the specified number of characters
    /// </summary>
    /// <param name="length"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithTrimFilenames(int length)
    {
        if (length < 10)
            throw new ArgumentOutOfRangeException(
                nameof(length),
                "Length should be at least 10 characters"
            );

        return AddOption("--trim-filenames", length.ToString());
    }

    /// <summary>
    /// Do not overwrite any files
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoOverwrites() => AddFlag("--no-overwrites");

    /// <summary>
    /// Overwrite all video and metadata files. This option includes <see cref="WithNoContinue" />
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithForceOverwrites() => AddFlag("--force-overwrites");

    /// <summary>
    /// Do not resume partially downloaded fragments. If the file is not fragmented, restart download of the entire file
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoContinue() => AddFlag("--no-continue");

    /// <summary>
    /// Do not use .part files - write directly into output file
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoPart() => AddFlag("--no-part");

    /// <summary>
    /// Use the Last-modified header to set the file modification time
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithMtime() => AddFlag("--mtime");

    /// <summary>
    /// Write video description to a .description file
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithWriteDescription() => AddFlag("--write-description");

    /// <summary>
    /// Write video metadata to a .info.json file (this may contain personal information)
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithWriteInfoJson() => AddFlag("--write-info-json");

    /// <summary>
    /// Do not write playlist metadata when using <see cref="WithWriteInfoJson"/>, <see cref="WithWriteDescription"/>
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoWritePlaylistMetafiles() => AddFlag("--no-write-playlist-metafiles");

    /// <summary>
    /// Write all fields to the infojson
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoCleanInfoJson() => AddFlag("--no-clean-info-json");

    /// <summary>
    /// Retrieve video comments to be placed in the infojson. The comments are fetched even without this option if the extraction is known to be quick
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithWriteComments() => AddFlag("--write-comments");

    /// <summary>
    /// Do not retrieve video comments unless the extraction is known to be quick
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoWriteComments() => AddFlag("--no-write-comments");

    /// <summary>
    /// JSON file containing the video information (created with the WriteVideoMetadata() option)
    /// </summary>
    /// <param name="path">*.json</param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithLoadInfoJson(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Json file path cannot be empty.", nameof(path));
        return AddOption("--load-info-json", path);
    }

    /// <summary>
    /// Netscape formatted file to read cookies from and dump cookie jar in
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public YtDlp WithCookiesFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(path));
        return new YtDlp(this, cookiesFile: Path.GetFullPath(path));
    }

    /// <summary>
    /// The name of the browser to load cookies from. Currently supported browsers are: brave, chrome, chromium, edge, firefox, opera, safari, vivaldi, whale.
    /// Optionally, the KEYRING used for decrypting Chromium cookies on Linux, the name/path of the PROFILE to load cookies from, and the CONTAINER name (if Firefox)
    /// ("none" for no container) can be given with their respective separators. By default, all containers of the most recently accessed profile are used.
    /// keyrings are: basictext, gnomekeyring, kwallet, kwallet5, kwallet6
    /// </summary>
    /// <param name="browser"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithCookiesFromBrowser(string browser) =>
        new YtDlp(this, cookiesFromBrowser: browser);

    /// <summary>
    /// Disable filesystem caching
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoCacheDir() => AddFlag("--no-cache-dir");

    /// <summary>
    /// Delete all filesystem cache files
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithRemoveCacheDir() => AddFlag("--rm-cache-dir");
}
