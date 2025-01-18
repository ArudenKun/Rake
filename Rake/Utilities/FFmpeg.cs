using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Builders;
using CliWrap.Exceptions;
using Gress;
using JetBrains.Annotations;
using Rake.Core.Downloading;
using Rake.Extensions;
using Rake.Helpers;
using Rake.Utilities.Downloading;

namespace Rake.Utilities;

[PublicAPI]
public static partial class FFmpeg
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static HttpClient HttpClient => HttpHelper.HttpClient;
    public static string CliFileName => OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    public static ValueTask ExecuteAsync(
        IEnumerable<string> arguments,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default
    ) => ExecuteAsync(builder => builder.Add(arguments), progress, cancellationToken);

    public static ValueTask ExecuteAsync(
        Action<ArgumentsBuilder> argumentsAction,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var builder = new ArgumentsBuilder();
        argumentsAction(builder);
        return ExecuteAsync(builder.Build(), progress, cancellationToken);
    }

    public static async ValueTask ExecuteAsync(
        string arguments,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var stdErrBuffer = new StringBuilder();
        var stdErrPipe = PipeTarget.Merge(
            // Collect error output in case of failure
            PipeTarget.ToStringBuilder(stdErrBuffer),
            // Collect progress output if requested
            progress?.Pipe(CreateProgressRouter) ?? PipeTarget.Null
        );

        try
        {
            await Cli.Wrap(GetCliFilePath())
                .WithArguments(arguments)
                .WithStandardErrorPipe(stdErrPipe)
                .ExecuteAsync(cancellationToken);
        }
        catch (CommandExecutionException ex)
        {
            throw new InvalidOperationException(
                $"""
                FFmpeg command-line tool failed with an error.

                Standard error:
                {stdErrBuffer}
                """,
                ex
            );
        }
    }

    public static async ValueTask ConcatenateAsync(
        IEnumerable<string> files,
        string filePath,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var tempFile = PathHelper.GetTempFilePath();
        try
        {
            var tempContent = string.Join(
                Environment.NewLine,
                files.Select(f => $"file '{f.Replace("'", "\\'")}'")
            );
            await File.WriteAllTextAsync(tempFile, tempContent, Utf8NoBom, cancellationToken);
            await ExecuteAsync(
                [
                    "-y",
                    "-f",
                    "concat",
                    "-fflags",
                    "+genpts",
                    "-async",
                    "1",
                    "-safe",
                    "0",
                    "-i",
                    tempFile,
                    "-c",
                    "copy",
                    filePath,
                ],
                progress,
                cancellationToken
            );
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    public static bool IsBundled() =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, CliFileName));

    public static bool IsAvailable() => !string.IsNullOrWhiteSpace(TryGetCliFilePath());

    public static string? TryGetCliFilePath(params string[] customPathsToProbe)
    {
        return GetProbeDirectoryPaths(customPathsToProbe)
            .Distinct(StringComparer.Ordinal)
            .Select(dirPath => Path.Combine(dirPath, CliFileName))
            .FirstOrDefault(File.Exists);

        static IEnumerable<string> GetProbeDirectoryPaths(string[] customPathsToProbe)
        {
            yield return AppContext.BaseDirectory;
            yield return Directory.GetCurrentDirectory();

            foreach (var customPath in customPathsToProbe)
                yield return customPath;

            // Process PATH
            if (
                Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) is
                { } processPaths
            )
            {
                foreach (var path in processPaths)
                    yield return path;
            }

            // Registry-based PATH variables
            if (OperatingSystem.IsWindows())
            {
                // User PATH
                if (
                    Environment
                        .GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
                        ?.Split(Path.PathSeparator) is
                    { } userPaths
                )
                {
                    foreach (var path in userPaths)
                        yield return path;
                }

                // System PATH
                if (
                    Environment
                        .GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)
                        ?.Split(Path.PathSeparator) is
                    { } systemPaths
                )
                {
                    foreach (var path in systemPaths)
                        yield return path;
                }
            }
        }
    }

    public static async Task<bool> DownloadAsync(
        IProgress<ICopyProgress>? progress = null,
        Action? extractingAction = null,
        CancellationToken cancellationToken = default
    )
    {
        if (IsAvailable() || IsBundled())
        {
            progress?.Report(CopyProgress.Complete);
            return true;
        }

        await using var jsonStream = await HttpClient.GetStreamAsync(
            "https://ffbinaries.com/api/v1/version/latest",
            cancellationToken
        );
        using var json = await JsonDocument.ParseAsync(
            jsonStream,
            cancellationToken: cancellationToken
        );
        var binJsonProperty = json.RootElement.GetProperty("bin");

        string platform;

        if (OperatingSystem.IsWindows())
        {
            platform = "windows-64";
        }
        else if (OperatingSystem.IsLinux())
        {
            platform = "linux-64";
        }
        else if (OperatingSystem.IsMacOS())
        {
            platform = "osx-64";
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }

        var downloadUrl =
            binJsonProperty.GetProperty(platform).GetProperty("ffmpeg").GetString() ?? string.Empty;
        await using var zipContentStream = await HttpClient.GetStreamAsync(
            downloadUrl,
            progress: progress,
            cancellationToken
        );
        extractingAction?.Invoke();
        await ZipFileHelper.ExtractFileAsync(
            zipContentStream,
            PathHelper.AppRootDirectory.Combine(CliFileName)
        );
        return TryGetCliFilePath() is not null;
    }

    private static string GetCliFilePath() =>
        TryGetCliFilePath()
        ?? throw new InvalidOperationException("FFmpeg command-line tool is not found.");

    private static PipeTarget CreateProgressRouter(IProgress<Percentage> progress)
    {
        var totalDuration = default(TimeSpan?);

        return PipeTarget.ToDelegate(line =>
        {
            // Extract total stream duration
            if (totalDuration is null)
            {
                // Need to extract all components separately because TimeSpan cannot directly
                // parse a time string that is greater than 24 hours.
                var totalDurationMatch = DurationRegex.Match(line);
                if (totalDurationMatch.Success)
                {
                    var hours = int.Parse(
                        totalDurationMatch.Groups[1].Value,
                        CultureInfo.InvariantCulture
                    );
                    var minutes = int.Parse(
                        totalDurationMatch.Groups[2].Value,
                        CultureInfo.InvariantCulture
                    );
                    var seconds = double.Parse(
                        totalDurationMatch.Groups[3].Value,
                        CultureInfo.InvariantCulture
                    );

                    totalDuration =
                        TimeSpan.FromHours(hours)
                        + TimeSpan.FromMinutes(minutes)
                        + TimeSpan.FromSeconds(seconds);
                }
            }

            if (totalDuration is null || totalDuration == TimeSpan.Zero)
            {
                progress.Report(Percentage.FromFraction(1));
                return;
            }

            // Extract processed stream duration
            var processedDurationMatch = TimeRegex.Match(line);
            if (processedDurationMatch.Success)
            {
                var hours = int.Parse(
                    processedDurationMatch.Groups[1].Value,
                    CultureInfo.InvariantCulture
                );
                var minutes = int.Parse(
                    processedDurationMatch.Groups[2].Value,
                    CultureInfo.InvariantCulture
                );
                var seconds = double.Parse(
                    processedDurationMatch.Groups[3].Value,
                    CultureInfo.InvariantCulture
                );

                var processedDuration =
                    TimeSpan.FromHours(hours)
                    + TimeSpan.FromMinutes(minutes)
                    + TimeSpan.FromSeconds(seconds);

                progress.Report(
                    Percentage.FromFraction(
                        (
                            processedDuration.TotalMilliseconds
                            / totalDuration.Value.TotalMilliseconds
                        ).Clamp(0, 1)
                    )
                );
            }
        });
    }

    [GeneratedRegex(@"Duration:\s(\d+):(\d+):(\d+\.\d+)")]
    private static partial Regex DurationRegex { get; }

    [GeneratedRegex(@"time=(\d+):(\d+):(\d+\.\d+)")]
    private static partial Regex TimeRegex { get; }
}
