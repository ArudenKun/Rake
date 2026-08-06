using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoInterfaceAttributes;
using Gress;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerKit;
using PowerKit.Extensions;
using Volo.Abp.Guids;
using Volo.Abp.IO;

namespace Rake.Core;

[AutoInterface]
public partial class ToolsService : IToolsService
{
    private const RegexOptions DefaultRegexOptions = RegexOptions.Compiled;

    [GeneratedRegex(@"\d+\.\d+\.\d+", DefaultRegexOptions)]
    private static partial Regex DenoVersionRegex();

    [GeneratedRegex(@"\d{4}\.\d{2}\.\d{2}", DefaultRegexOptions)]
    private static partial Regex YtDlpVersionRegex();

    [GeneratedRegex(@"version\s+([^\s]+)", DefaultRegexOptions)]
    private static partial Regex FFmpegVersionRegex();

    [GeneratedRegex(@"\d+\.\d+\.\d+", DefaultRegexOptions)]
    private static partial Regex Aria2VersionRegex();

    public ToolsService(
        IOptions<RakeCoreOptions> options,
        HttpClient httpClient,
        IGuidGenerator guidGenerator,
        ILogger<ToolsService> logger
    )
    {
        Options = options.Value;
        HttpClient = httpClient;
        GuidGenerator = guidGenerator;
        Logger = logger;
    }

    protected RakeCoreOptions Options { get; }
    protected HttpClient HttpClient { get; }
    protected IGuidGenerator GuidGenerator { get; }
    protected ILogger<ToolsService> Logger { get; }

    public bool IsAvailable(Tool tool) => IsLocalAvailable(tool) || IsPathAvailable(tool);

    public bool IsLocalAvailable(Tool tool) => File.Exists(GetLocalPath(tool));

    public bool IsPathAvailable(Tool tool) => !string.IsNullOrEmpty(GetSystemPath(tool));

    public string GetLocalPath(Tool tool)
    {
        var binaryName = GetBinaryName(tool);
        if (string.IsNullOrEmpty(binaryName))
            return string.Empty;

        var fullToolsDir = Path.GetFullPath(Options.ToolsDirectory);
        return Path.Combine(fullToolsDir, binaryName);
    }

    public string GetPath(Tool tool)
    {
        if (IsLocalAvailable(tool))
            return GetLocalPath(tool);

        return GetSystemPath(tool);
    }

    public string GetSystemPath(Tool tool)
    {
        var binaryName = GetBinaryName(tool);
        return string.IsNullOrEmpty(binaryName) ? string.Empty : FindInPath(binaryName);
    }

    /// <summary>
    /// Executes the tool binary with its version flag and retrieves the parsed version string.
    /// </summary>
    public async Task<string> GetVersionAsync(
        Tool tool,
        CancellationToken cancellationToken = default
    )
    {
        var executablePath = GetPath(tool);
        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
        {
            Logger.LogWarning("Cannot check version for {Tool}. Executable path not found.", tool);
            return string.Empty;
        }

        var versionArgument = tool switch
        {
            Tool.FFmpeg => "-version",
            _ => "--version",
        };

        Logger.LogDebug(
            "Executing '{Path} {Argument}' to determine version for {Tool}.",
            executablePath,
            versionArgument,
            tool
        );

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = versionArgument,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process();
        process.StartInfo = startInfo;

        try
        {
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var rawOutput = await outputTask;
            if (string.IsNullOrWhiteSpace(rawOutput))
            {
                rawOutput = await errorTask;
            }

            var version = ParseVersionString(tool, rawOutput);
            if (string.IsNullOrWhiteSpace(version))
            {
                Logger.LogWarning(
                    "Failed to parse version output for {Tool}. Raw output: '{RawOutput}'",
                    tool,
                    rawOutput.Trim()
                );
            }
            else
            {
                Logger.LogInformation("Resolved version for {Tool}: {Version}", tool, version);
            }

            return version;
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "An error occurred while getting version for {Tool} at '{Path}'.",
                tool,
                executablePath
            );
            return string.Empty;
        }
    }

    /// <summary>
    /// Downloads the specified tool binary and saves it to the local tools directory.
    /// For FFmpeg, both ffmpeg and ffprobe binaries are extracted together.
    /// </summary>
    public async Task DownloadAsync(
        Tool tool,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        Logger.LogInformation("Starting download for {Tool}.", tool);

        var destinationPath = GetLocalPath(tool);
        if (string.IsNullOrEmpty(destinationPath))
        {
            Logger.LogError("Failed to resolve local path for {Tool}.", tool);
            throw new InvalidOperationException($"Unsupported tool: {tool}");
        }

        var toolsDirectory = Path.GetDirectoryName(destinationPath)!;
        DirectoryHelper.CreateIfNotExists(toolsDirectory);

        var downloadUrl = await GetDownloadUrlAsync(tool, cancellationToken);
        Logger.LogInformation("Downloading {Tool} from URL: '{Url}'", tool, downloadUrl);

        switch (tool)
        {
            case Tool.YtDlp:
            case Tool.Aria2:
                await DownloadFileAsync(downloadUrl, destinationPath, progress, cancellationToken);
                break;

            case Tool.Deno:
            case Tool.FFmpeg:
                await DownloadAndExtractZipAsync(
                    downloadUrl,
                    toolsDirectory,
                    tool,
                    progress,
                    cancellationToken
                );
                break;

            default:
                Logger.LogError("Attempted to download unsupported tool: {Tool}", tool);
                throw new NotSupportedException(
                    $"Tool type '{tool}' is not supported for download."
                );
        }

        // Grant Unix execution permissions for local files
        if (tool == Tool.FFmpeg)
        {
            SetUnixExecPerms(destinationPath);
            var ffprobePath = Path.Combine(
                toolsDirectory,
                OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe"
            );

            if (File.Exists(ffprobePath))
            {
                SetUnixExecPerms(ffprobePath);
            }
        }
        else
        {
            SetUnixExecPerms(destinationPath);
        }

        Logger.LogInformation("Successfully downloaded {Tool}", tool);
    }

    #region Helpers

    private string ParseVersionString(Tool tool, string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return string.Empty;

        var firstLine = rawOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstLine))
            return string.Empty;

        return tool switch
        {
            Tool.Deno => DenoVersionRegex().Match(firstLine).Value,
            Tool.YtDlp => YtDlpVersionRegex().Match(firstLine).Value,
            Tool.FFmpeg => MatchFFmpegVersion(firstLine),
            Tool.Aria2 => Aria2VersionRegex().Match(firstLine).Value,
            _ => firstLine.Trim(),
        };
    }

    private static string MatchFFmpegVersion(string line)
    {
        var match = FFmpegVersionRegex().Match(line);
        return match.Success ? match.Groups[1].Value : line.Trim();
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<Percentage>? progress,
        CancellationToken cancellationToken
    )
    {
        using var response = await HttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        Logger.LogDebug("Tool Download size: {TotalBytes} bytes.", totalBytes);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8192,
            true
        );

        await contentStream.CopyToAsync(
            fileStream,
            totalBytes,
            progress?.ToDoubleBased(),
            cancellationToken
        );

        Logger.LogDebug(
            "Successfully saved downloaded tool to '{DestinationPath}'.",
            destinationPath
        );
    }

    private async Task DownloadAndExtractZipAsync(
        string url,
        string outputDirectory,
        Tool tool,
        IProgress<Percentage>? progress,
        CancellationToken cancellationToken
    )
    {
        using var tempZipPath = new TempFile(
            Path.Combine(
                Path.GetFullPath(Options.ToolsDirectory),
                $"{GuidGenerator.Create()}_{tool}.zip"
            )
        );

        Logger.LogDebug(
            "Downloading temporary zip archive for {Tool} to '{TempPath}'.",
            tool,
            tempZipPath
        );

        await DownloadFileAsync(url, tempZipPath.Path, progress, cancellationToken);

        Logger.LogDebug("Opening zip archive '{TempPath}' for extraction.", tempZipPath.Path);
        await using var archive = await ZipFile.OpenReadAsync(tempZipPath.Path, cancellationToken);

        var targetNames = tool switch
        {
            Tool.FFmpeg => OperatingSystem.IsWindows()
                ? new[] { "ffmpeg.exe", "ffprobe.exe" }
                : new[] { "ffmpeg", "ffprobe" },
            _ => [GetBinaryName(tool)],
        };

        var extractedAny = false;
        foreach (var entry in archive.Entries)
        {
            var fileName = Path.GetFileName(entry.Name);
            if (!targetNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                continue;

            var destPath = Path.Combine(outputDirectory, fileName);
            Logger.LogInformation(
                "Extracting '{EntryName}' to '{DestPath}'.",
                entry.Name,
                destPath
            );

            await entry.ExtractToFileAsync(
                destPath,
                overwrite: true,
                cancellationToken: cancellationToken
            );
            extractedAny = true;
        }

        if (!extractedAny)
        {
            Logger.LogError(
                "Expected binaries [{TargetNames}] not found in archive for {Tool}.",
                string.Join(", ", targetNames),
                tool
            );
            throw new FileNotFoundException(
                $"Could not find target binary for '{tool}' inside the zip archive."
            );
        }
    }

    private async Task<string> GetDownloadUrlAsync(
        Tool tool,
        CancellationToken cancellationToken
    ) =>
        tool switch
        {
            Tool.YtDlp => GetYtDlpDownloadUrl(),
            Tool.Deno => GetDenoDownloadUrl(),
            Tool.FFmpeg => OperatingSystem.IsWindows()
                ? "https://github.com/Tyrrrz/FFmpegBin/releases/latest/download/ffmpeg-windows-x64.zip"
            : OperatingSystem.IsMacOS()
                ? "https://github.com/Tyrrrz/FFmpegBin/releases/latest/download/ffmpeg-osx-x64.zip"
            : "https://github.com/Tyrrrz/FFmpegBin/releases/latest/download/ffmpeg-linux-x64.zip",
            Tool.Aria2 => await GetAria2NextLatestDownloadUrlAsync(cancellationToken),
            _ => throw new NotSupportedException($"No download URL configured for '{tool}'."),
        };

    private static string GetYtDlpDownloadUrl()
    {
        var isArm64 = RuntimeInformation.ProcessArchitecture is Architecture.Arm64;

        if (OperatingSystem.IsWindows())
            return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

        if (OperatingSystem.IsMacOS())
            return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos";

        return isArm64
            ? "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_aarch64"
            : "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp";
    }

    private static string GetDenoDownloadUrl()
    {
        var isArm64 = RuntimeInformation.ProcessArchitecture is Architecture.Arm64;

        if (OperatingSystem.IsWindows())
            return "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";

        if (OperatingSystem.IsMacOS())
            return isArm64
                ? "https://github.com/denoland/deno/releases/latest/download/deno-aarch64-apple-darwin.zip"
                : "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-apple-darwin.zip";

        return isArm64
            ? "https://github.com/denoland/deno/releases/latest/download/deno-aarch64-unknown-linux-gnu.zip"
            : "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-unknown-linux-gnu.zip";
    }

    private async Task<string> GetAria2NextLatestDownloadUrlAsync(
        CancellationToken cancellationToken
    )
    {
        const string apiUrl =
            "https://api.github.com/repos/AnInsomniacy/aria2-next/releases/latest";

        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.UserAgent.ParseAdd(RakeConsts.Name);

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken
        );

        var platform =
            OperatingSystem.IsWindows() ? "windows"
            : OperatingSystem.IsMacOS() ? "macos"
            : "linux";

        var isArm64 = RuntimeInformation.ProcessArchitecture is Architecture.Arm64;

        var archKeyword = isArm64
            ? OperatingSystem.IsLinux()
                ? "aarch64"
                : "arm64"
            : "x86_64";

        if (
            json.RootElement.TryGetProperty("assets", out var assets)
            && assets.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (
                    string.IsNullOrEmpty(name)
                    || name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase)
                )
                    continue;

                if (
                    name.Contains(platform, StringComparison.OrdinalIgnoreCase)
                    && name.Contains(archKeyword, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                }
            }
        }

        throw new FileNotFoundException(
            $"Could not find a valid release asset for aria2-next on platform '{platform}' with architecture '{archKeyword}'."
        );
    }

    #endregion

    public static string GetBinaryName(Tool tool)
    {
        var isWindows = OperatingSystem.IsWindows();
        var isMac = OperatingSystem.IsMacOS();
        var isLinux = OperatingSystem.IsLinux();

        if (!isWindows && !isMac && !isLinux)
        {
            throw new PlatformNotSupportedException("Your Operating System is not supported.");
        }

        return tool switch
        {
            Tool.Deno => isWindows ? "deno.exe" : "deno",
            Tool.FFmpeg => isWindows ? "ffmpeg.exe" : "ffmpeg",
            Tool.YtDlp => isWindows ? "yt-dlp.exe"
            : isMac ? "yt-dlp_macos"
            : "yt-dlp",
            Tool.Aria2 => isWindows ? "aria2c.exe" : "aria2c",
            _ => string.Empty,
        };
    }

    private void SetUnixExecPerms(string filePath)
    {
        if (OperatingSystem.IsWindows())
            return;

        Logger.LogDebug(
            "Setting Unix executable permissions (755) for file: '{FilePath}'",
            filePath
        );

        File.SetUnixFileMode(
            filePath,
            UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite
        );
    }

    private static string FindInPath(string exe)
    {
        var rawPath = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(rawPath))
            return string.Empty;

        var paths = rawPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        return paths
                .Select(p => p.Trim('"', ' '))
                .Select(p => Path.Combine(p, exe))
                .FirstOrDefault(File.Exists)
            ?? string.Empty;
    }
}
