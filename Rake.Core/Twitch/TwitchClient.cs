using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CliWrap;
using Gress;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerKit;
using Rake.Core.Extensions;
using Rake.Core.Twitch.Clips;
using Rake.Core.Twitch.Videos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace Rake.Core.Twitch;

public partial class TwitchClient : ITransientDependency
{
    public TwitchClient(IAbpLazyServiceProvider lazyServiceProvider)
    {
        LazyServiceProvider = lazyServiceProvider;
    }

    protected IAbpLazyServiceProvider LazyServiceProvider { get; }

    protected RakeCoreOptions Options =>
        LazyServiceProvider.LazyGetRequiredService<IOptions<RakeCoreOptions>>().Value;

    protected ILoggerFactory LoggerFactory =>
        LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        LazyServiceProvider.LazyGetService<ILogger>(_ =>
            LoggerFactory.CreateLogger(GetType().FullName!)
        );

    protected IGuidGenerator GuidGenerator =>
        LazyServiceProvider.LazyGetRequiredService<IGuidGenerator>();

    protected IToolsService ToolsService =>
        LazyServiceProvider.LazyGetRequiredService<IToolsService>();

    public async Task<TwitchClip> GetClipAsync(
        TwitchId id,
        CancellationToken cancellationToken = default
    ) =>
        !id.IsClip
            ? throw new InvalidOperationException($"{id} is not a valid clip id")
            : ParseToTwitchClip(await GetAsyncInternal(id, cancellationToken));

    public async Task<TwitchVideo> GetVideoAsync(
        TwitchId id,
        CancellationToken cancellationToken = default
    ) =>
        !id.IsVideo
            ? throw new InvalidOperationException($"{id} is not a valid video id")
            : ParseToTwitchVideo(await GetAsyncInternal(id, cancellationToken));

    private async Task<string> GetAsyncInternal(
        TwitchId id,
        CancellationToken cancellationToken = default
    )
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        try
        {
            await Cli.Wrap(ToolsService.GetPath(Tool.TwitchDownloaderCli))
                .WithArguments(builder =>
                    builder.Add("info").Add("-u").Add(id).Add("-f").Add("Raw")
                )
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                .ExecuteAsync(cancellationToken);
            return stdOut.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Tool} Error: {Error}", Tool.TwitchDownloaderCli, stdErr);
            throw;
        }
    }

    public async Task DownloadVideoAsync(
        TwitchId id,
        TwitchFilePath outputPath,
        Action<TwitchVideoDownloadOptionsBuilder>? configureOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!id.IsVideo)
        {
            throw new InvalidOperationException($"{id} is not a valid video id");
        }
        var optionsBuilder = new TwitchVideoDownloadOptionsBuilder();
        configureOptions?.Invoke(optionsBuilder);
        var options = optionsBuilder.Build();
        var stdErr = new StringBuilder();
        string? currentStage = null;

        var tempDirectory = new TempDirectory(
            Options.ToolsDirectory.CombinePath($"{GuidGenerator.Create():N}")
        );
        try
        {
            var downloadProgress = new DownloadProgress(
                (long)options.Quality.FileSize.Bytes,
                args => options.DownloadProgress?.Invoke(args)
            );
            await Cli.Wrap(ToolsService.GetPath(Tool.TwitchDownloaderCli))
                .WithArguments(builder =>
                    builder
                        .Add("videodownload")
                        .Add("-u", id)
                        .Add("-o", outputPath)
                        .AddIfNotNullOrWhiteSpace("-q", options.Quality.Name)
                        .AddIf(
                            options.TrimBeginningTime.HasValue,
                            "-b",
                            $"{options.TrimBeginningTime?.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s"
                        )
                        .AddIf(
                            options.TrimEndingTime.HasValue,
                            "-e",
                            $"{options.TrimEndingTime?.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s"
                        )
                        .Add("-t", $"{options.Threads}")
                        .Add("--trim-mode", options.TrimMode.ToString())
                        .AddIfNotNullOrWhiteSpace("--oauth", options.OAuth)
                        .Add("--ffmpeg-path", ToolsService.GetPath(Tool.FFmpeg))
                        // ReSharper disable once AccessToDisposedClosure
                        .Add("--temp-path", tempDirectory.Path)
                        .Add("--collision", "Overwrite")
                )
                .WithStandardOutputPipe(
                    PipeTarget.ToDelegate(line =>
                    {
                        var match = VideoDownloadProgressRegex().Match(line);
                        if (!match.Success)
                            return;

                        var stage = match.Groups["stage"].Value;
                        if (
                            !int.TryParse(
                                match.Groups["percent"].Value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out var percentage
                            )
                        )
                            return;

                        var isNewStage = !string.Equals(
                            currentStage,
                            stage,
                            StringComparison.Ordinal
                        );

                        if (isNewStage)
                        {
                            // Fire completion for previous stage before switching
                            CompleteStage(currentStage);
                            currentStage = stage;

                            switch (stage)
                            {
                                case "Downloading":
                                    options.DownloadStarted?.Invoke();
                                    break;
                                case "Verifying Parts":
                                    options.VerifyStarted?.Invoke();
                                    break;
                                case "Finalizing Video":
                                    options.FinalizingStarted?.Invoke();
                                    break;
                            }
                        }

                        switch (stage)
                        {
                            case "Downloading":
                                downloadProgress.Report(Percentage.FromValue(percentage).Fraction);
                                break;
                            case "Verifying Parts":
                                options.VerifyProgress?.Invoke(percentage);
                                break;
                            case "Finalizing Video":
                                options.FinalizingProgress?.Invoke(percentage);
                                break;
                        }
                    })
                )
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                .ExecuteAsync(cancellationToken);

            // Fire completion for the final stage once execution succeeds
            CompleteStage(currentStage);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Tool} Error: {Error}", Tool.TwitchDownloaderCli, stdErr);
            throw;
        }
        finally
        {
            tempDirectory.Dispose();
        }

        return;

        void CompleteStage(string? stage)
        {
            switch (stage)
            {
                case "Downloading":
                    options.DownloadCompleted?.Invoke();
                    break;
                case "Verifying Parts":
                    options.VerifyCompleted?.Invoke();
                    break;
                case "Finalizing Video":
                    options.FinalizingCompleted?.Invoke();
                    break;
            }
        }
    }

    public async Task DownloadClipAsync(
        TwitchId id,
        TwitchFilePath outputPath,
        Action<TwitchClipDownloadOptionsBuilder>? configureOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!id.IsClip)
        {
            throw new InvalidOperationException($"{id} is not a valid clip id");
        }
        var optionsBuilder = new TwitchClipDownloadOptionsBuilder();
        configureOptions?.Invoke(optionsBuilder);
        var options = optionsBuilder.Build();
        var stdErr = new StringBuilder();
        string? currentStage = null;
        var tempDirectory = new TempDirectory(
            Options.ToolsDirectory.CombinePath($"{GuidGenerator.Create():N}")
        );
        try
        {
            var downloadProgress = new DownloadProgress(
                (long)options.Quality.FileSize.Bytes,
                args => options.DownloadProgress?.Invoke(args)
            );
            await Cli.Wrap(ToolsService.GetPath(Tool.TwitchDownloaderCli))
                .WithArguments(builder =>
                    builder
                        .Add("clipdownload")
                        .Add("-u", id)
                        .Add("-o", outputPath)
                        .AddIfNotNullOrWhiteSpace("-q", options.Quality.Name)
                        .Add("--encode-metadata", $"{options.EncodeMetadata}")
                        .Add("--ffmpeg-path", ToolsService.GetPath(Tool.FFmpeg))
                        .Add("--temp-path", Options.ToolsDirectory.CombinePath("temp"))
                        .Add("--collision", "Overwrite")
                )
                .WithStandardOutputPipe(
                    PipeTarget.ToDelegate(line =>
                    {
                        var match = ClipDownloadProgressRegex().Match(line);
                        if (!match.Success)
                            return;

                        var stage = match.Groups["stage"].Value;
                        if (
                            !int.TryParse(
                                match.Groups["percent"].Value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out var percentage
                            )
                        )
                            return;

                        var isNewStage = !string.Equals(
                            currentStage,
                            stage,
                            StringComparison.Ordinal
                        );

                        if (isNewStage)
                        {
                            // Fire completion for previous stage before switching
                            CompleteStage(currentStage);
                            currentStage = stage;

                            switch (stage)
                            {
                                case "Downloading Clip":
                                    options.DownloadStarted?.Invoke();
                                    break;

                                case "Encoding Clip Metadata":
                                    options.EncodingStarted?.Invoke();
                                    break;
                            }
                        }

                        switch (stage)
                        {
                            case "Downloading Clip":
                                downloadProgress.Report(Percentage.FromValue(percentage).Fraction);
                                break;

                            case "Encoding Clip Metadata":
                                options.EncodingProgress?.Invoke(percentage);
                                break;
                        }
                    })
                )
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                .ExecuteAsync(cancellationToken);

            // Fire completion for the final stage once execution succeeds
            CompleteStage(currentStage);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Tool} Error: {Error}", Tool.TwitchDownloaderCli, stdErr);
            throw;
        }
        finally
        {
            tempDirectory.Dispose();
        }

        return;

        void CompleteStage(string? stage)
        {
            switch (stage)
            {
                case "Downloading Clip":
                    options.DownloadCompleted?.Invoke();
                    break;

                case "Encoding Clip Metadata":
                    options.EncodingCompleted?.Invoke();
                    break;
            }
        }
    }

    private static TwitchClip ParseToTwitchClip(string rawOutput)
    {
        using var reader = new StringReader(rawOutput);
        while (reader.ReadLine() is { } line)
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith('{'))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(trimmedLine);
                var root = doc.RootElement;

                if (
                    root.TryGetProperty("data", out var data)
                    && data.TryGetProperty("clip", out var clip)
                )
                {
                    var title = clip.TryGetProperty("title", out var titleProp)
                        ? titleProp.GetString() ?? ""
                        : "";
                    var durationSeconds = clip.TryGetProperty("durationSeconds", out var durProp)
                        ? durProp.GetInt32()
                        : 0;
                    var viewCount = clip.TryGetProperty("viewCount", out var viewsProp)
                        ? viewsProp.GetInt32()
                        : 0;
                    var thumbnailUrl = clip.TryGetProperty("thumbnailURL", out var thumbProp)
                        ? thumbProp.GetString() ?? ""
                        : "";

                    var createdAt = DateTime.MinValue;
                    if (
                        clip.TryGetProperty("createdAt", out var createdProp)
                        && DateTimeOffset.TryParse(createdProp.GetString(), out var dt)
                    )
                    {
                        createdAt = dt.UtcDateTime;
                    }

                    TwitchOwner broadcaster = new("", "", "");
                    if (
                        clip.TryGetProperty("broadcaster", out var bProp)
                        && bProp.ValueKind != JsonValueKind.Null
                    )
                    {
                        broadcaster = bProp.Deserialize<TwitchOwner>() ?? broadcaster;
                    }

                    TwitchGame game = new("", "", "");
                    if (
                        clip.TryGetProperty("game", out var gProp)
                        && gProp.ValueKind != JsonValueKind.Null
                    )
                    {
                        game = gProp.Deserialize<TwitchGame>() ?? game;
                    }

                    var qualities = new List<TwitchClipQuality>();
                    if (
                        clip.TryGetProperty("assets", out var assetsProp)
                        && assetsProp.ValueKind == JsonValueKind.Array
                    )
                    {
                        foreach (var asset in assetsProp.EnumerateArray())
                        {
                            if (
                                asset.TryGetProperty("videoQualities", out var vqProp)
                                && vqProp.ValueKind == JsonValueKind.Array
                            )
                            {
                                foreach (var vq in vqProp.EnumerateArray())
                                {
                                    var qualityStr = vq.TryGetProperty("quality", out var q)
                                        ? q.GetString() ?? ""
                                        : "";
                                    var width = vq.TryGetProperty("width", out var w)
                                        ? w.GetInt32()
                                        : 0;
                                    var height = vq.TryGetProperty("height", out var h)
                                        ? h.GetInt32()
                                        : 0;
                                    var bitrate = vq.TryGetProperty("bitrate", out var b)
                                        ? b.GetInt64()
                                        : 0;
                                    var codecsStr = vq.TryGetProperty("codecs", out var c)
                                        ? c.GetString() ?? ""
                                        : "";

                                    int? fps = null;
                                    if (vq.TryGetProperty("frameRate", out var fr))
                                    {
                                        fps = (int)Math.Round(fr.GetDouble());
                                    }

                                    var codecs = codecsStr
                                        .Split(
                                            ',',
                                            StringSplitOptions.RemoveEmptyEntries
                                                | StringSplitOptions.TrimEntries
                                        )
                                        .ToList();

                                    var calculatedBytes = bitrate * durationSeconds / 8;
                                    var fileSize = ByteSize.FromBytes(calculatedBytes);

                                    // var name = fps is > 30
                                    //     ? $"{qualityStr}p{fps}"
                                    //     : $"{qualityStr}p";
                                    // if (string.IsNullOrWhiteSpace(qualityStr))
                                    // {
                                    //     name = $"{height}p";
                                    // }

                                    qualities.Add(
                                        new TwitchClipQuality(
                                            Name: qualityStr,
                                            Width: width,
                                            Height: height,
                                            Fps: fps,
                                            Codecs: codecs,
                                            Bitrate: bitrate,
                                            FileSize: fileSize
                                        )
                                    );
                                }
                                break;
                            }
                        }
                    }

                    var offsetSeconds = clip.TryGetProperty("videoOffsetSeconds", out var ofProp)
                        ? ofProp.GetInt64()
                        : 0;

                    return new TwitchClip(
                        Title: title,
                        DurationSeconds: durationSeconds,
                        ThumbnailUrl: thumbnailUrl,
                        Broadcaster: broadcaster,
                        Game: game,
                        Views: viewCount,
                        CreatedAt: createdAt,
                        Qualities: qualities,
                        OffsetSeconds: offsetSeconds
                    );
                }
            }
            catch (JsonException)
            {
                // Skip non-JSON lines
            }
        }

        throw new InvalidOperationException(
            "Failed to parse Twitch clip metadata from CLI output."
        );
    }

    private static TwitchVideo ParseToTwitchVideo(string rawOutput)
    {
        var title = string.Empty;
        long lengthSeconds = 0;
        var viewCount = 0;
        var createdAt = DateTime.MinValue;
        var description = string.Empty;

        var thumbnailUrls = new List<string>();
        TwitchOwner owner = new(string.Empty, string.Empty, string.Empty);
        TwitchGame game = new(string.Empty, string.Empty, string.Empty);
        var chapters = new List<TwitchVideoChapter>();

        // 1. Parse JSON objects for Video metadata and Moments/Chapters
        using var reader = new StringReader(rawOutput);
        while (reader.ReadLine() is { } line)
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith('{'))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(trimmedLine);
                var root = doc.RootElement;

                if (
                    root.TryGetProperty("data", out var data)
                    && data.TryGetProperty("video", out var video)
                )
                {
                    // Metadata JSON block
                    if (video.TryGetProperty("title", out var titleProp))
                    {
                        title = titleProp.GetString() ?? string.Empty;
                        lengthSeconds = video.TryGetProperty("lengthSeconds", out var len)
                            ? len.GetInt64()
                            : 0;
                        viewCount = video.TryGetProperty("viewCount", out var views)
                            ? views.GetInt32()
                            : 0;
                        description =
                            video.TryGetProperty("description", out var desc)
                            && desc.ValueKind != JsonValueKind.Null
                                ? desc.GetString() ?? string.Empty
                                : string.Empty;

                        if (
                            video.TryGetProperty("createdAt", out var created)
                            && DateTimeOffset.TryParse(created.GetString(), out var dt)
                        )
                        {
                            createdAt = dt.UtcDateTime;
                        }

                        // Deserialize Thumbnail URLs
                        if (
                            video.TryGetProperty("thumbnailURLs", out var thumbsElement)
                            && thumbsElement.ValueKind == JsonValueKind.Array
                        )
                        {
                            var parsedUrls = thumbsElement.Deserialize<List<string>>();
                            if (parsedUrls != null)
                            {
                                thumbnailUrls.AddRange(parsedUrls);
                            }
                        }

                        // Deserialize Owner object directly
                        if (
                            video.TryGetProperty("owner", out var ownerElement)
                            && ownerElement.ValueKind != JsonValueKind.Null
                        )
                        {
                            var parsedOwner = ownerElement.Deserialize<TwitchOwner>();
                            if (parsedOwner != null)
                            {
                                owner = parsedOwner;
                            }
                        }

                        // Deserialize Game object directly
                        if (
                            video.TryGetProperty("game", out var gameElement)
                            && gameElement.ValueKind != JsonValueKind.Null
                        )
                        {
                            var parsedGame = gameElement.Deserialize<TwitchGame>();
                            if (parsedGame != null)
                            {
                                game = parsedGame;
                            }
                        }
                    }

                    // Chapters/Moments JSON block
                    if (
                        video.TryGetProperty("moments", out var moments)
                        && moments.TryGetProperty("edges", out var edges)
                    )
                    {
                        foreach (var edge in edges.EnumerateArray())
                        {
                            if (edge.TryGetProperty("node", out var node))
                            {
                                var chapterDesc = node.TryGetProperty("description", out var d)
                                    ? d.GetString() ?? string.Empty
                                    : string.Empty;
                                var type = node.TryGetProperty("type", out var t)
                                    ? t.GetString() ?? string.Empty
                                    : string.Empty;
                                var posMs = node.TryGetProperty("positionMilliseconds", out var pos)
                                    ? pos.GetInt32()
                                    : 0;
                                var durMs = node.TryGetProperty("durationMilliseconds", out var dur)
                                    ? dur.GetInt32()
                                    : 0;

                                var startSec = posMs / 1000;
                                var lengthSec = durMs / 1000;

                                chapters.Add(
                                    new TwitchVideoChapter(
                                        Category: chapterDesc,
                                        Type: type,
                                        StartSeconds: startSec,
                                        EndSeconds: startSec + lengthSec,
                                        LengthSeconds: lengthSec
                                    )
                                );
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Skip non-JSON lines
            }
        }

        // 2. Parse streams directly from the M3U8 Master Playlist section using lengthSeconds to calculate FileSize
        var qualities = ParseM3u8Streams(rawOutput, lengthSeconds);

        return new TwitchVideo(
            Title: title,
            DurationSeconds: lengthSeconds,
            ThumbnailUrls: thumbnailUrls,
            Owner: owner,
            Game: game,
            Views: viewCount,
            CreatedAt: createdAt,
            Qualities: qualities,
            Chapters: chapters,
            Description: description
        );
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static List<TwitchVideoQuality> ParseM3u8Streams(string m3u8Content, long lengthSeconds)
    {
        var streams = new List<TwitchVideoQuality>();

        var mediaMatches = M3u8MediaRegex().Matches(m3u8Content);
        var streamInfMatches = M3u8StreamInfRegex().Matches(m3u8Content);

        for (var i = 0; i < streamInfMatches.Count; i++)
        {
            var streamMatch = streamInfMatches[i];
            var attrs = streamMatch.Groups["attrs"].Value;

            // Name
            var name =
                i < mediaMatches.Count ? mediaMatches[i].Groups["name"].Value : $"Stream_{i + 1}";

            // Bandwidth
            long bandwidth = 0;
            var bwMatch = BandwidthRegex().Match(attrs);
            if (bwMatch.Success)
                long.TryParse(
                    bwMatch.Groups["bw"].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out bandwidth
                );

            // Compute total bytes: (bandwidth in bits/sec * length in seconds) / 8 bits per byte
            var calculatedBytes = bandwidth * lengthSeconds / 8;
            var fileSize = ByteSize.FromBytes(calculatedBytes);

            // Codecs
            var codecs = new List<string>();
            var codecMatch = CodecRegex().Match(attrs);
            if (codecMatch.Success)
            {
                codecs =
                [
                    .. codecMatch
                        .Groups["codecs"]
                        .Value.Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        ),
                ];
            }

            // Resolution
            var resolution = "N/A";
            var resMatch = ResolutionRegex().Match(attrs);
            if (resMatch.Success)
                resolution = resMatch.Groups["res"].Value;

            // FPS
            int? fps = null;
            var fpsMatch = FrameRateRegex().Match(attrs);
            if (
                fpsMatch.Success
                && double.TryParse(
                    fpsMatch.Groups["fps"].Value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var fpsDouble
                )
            )
            {
                fps = (int)Math.Round(fpsDouble);
            }

            streams.Add(
                new TwitchVideoQuality(
                    Name: name,
                    Resolution: resolution,
                    Fps: fps,
                    Codecs: codecs,
                    Bitrate: bandwidth,
                    FileSize: fileSize
                )
            );
        }

        return streams;
    }

    #region VideoDownloadPattens

    [GeneratedRegex(
        @"\[STATUS\]\s*-\s*(?<stage>Downloading|Verifying Parts|Finalizing Video)\s+(?<percent>\d{1,3})%\s*\[(?<current>\d+)/(?<total>\d+)\]"
    )]
    private static partial Regex VideoDownloadProgressRegex();

    #endregion

    #region ClipDownloadPatterns

    [GeneratedRegex(
        @"\[STATUS\]\s*-\s*(?<stage>Downloading Clip|Encoding Clip Metadata)\s+(?<percent>\d{1,3})%"
    )]
    private static partial Regex ClipDownloadProgressRegex();

    #endregion

    #region InfoPatterns

    // Regex for matching M3U8 stream tags
    [GeneratedRegex(@"#EXT-X-MEDIA:[^\r\n]*?NAME=""(?<name>[^""]+)""")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static partial Regex M3u8MediaRegex();

    [GeneratedRegex(@"#EXT-X-STREAM-INF:(?<attrs>[^\r\n]+)")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static partial Regex M3u8StreamInfRegex();

    [GeneratedRegex(@"BANDWIDTH=(?<bw>\d+)")]
    private static partial Regex BandwidthRegex();

    [GeneratedRegex(@"CODECS=""(?<codecs>[^""]+)""")]
    private static partial Regex CodecRegex();

    [GeneratedRegex(@"RESOLUTION=(?<res>\d+x\d+)")]
    private static partial Regex ResolutionRegex();

    [GeneratedRegex(@"FRAME-RATE=(?<fps>[\d\.]+)")]
    private static partial Regex FrameRateRegex();

    #endregion
}
