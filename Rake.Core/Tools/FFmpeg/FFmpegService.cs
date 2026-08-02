using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AutoInterfaceAttributes;
using Octokit;
using Volo.Abp.DependencyInjection;

namespace Rake.Core.Tools.FFmpeg;

[AutoInterface]
internal partial class FFmpegToolsService : ToolsServiceBase, IFFmpegToolsService
{
    public FFmpegToolsService(IAbpLazyServiceProvider lazyServiceProvider, HttpClient httpClient)
        : base(lazyServiceProvider)
    {
        HttpClient = httpClient;
    }

    protected HttpClient HttpClient { get; }

    protected override string RepositoryOwner => "Tyrrrz";
    protected override string RepositoryName => "FFmpegBin";

    public bool IsAvailable(string targetPath) => IsAvailable(targetPath, "ffmpeg");

    public bool IsLocalAvailable(string targetPath) => IsLocalAvailable(targetPath, "ffmpeg");

    public async Task<string> GetVersionAsync(
        string targetPath,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetExecutablePath(targetPath, out var resolvedPath))
        {
            return string.Empty;
        }

        var version = await GetInstalledFFmpegVersionAsync(resolvedPath, cancellationToken);
        return version ?? string.Empty;
    }

    public async Task<bool> IsLatestVersionAsync(
        string targetPath,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetExecutablePath(targetPath, out var resolvedPath))
        {
            return false;
        }

        var installedVersion = await GetInstalledFFmpegVersionAsync(
            resolvedPath,
            cancellationToken
        );
        if (string.IsNullOrWhiteSpace(installedVersion))
            return false;

        try
        {
            var latestRelease = await GitHubClient.Repository.Release.GetLatest(
                RepositoryOwner,
                RepositoryName
            );

            return installedVersion.Contains(
                    latestRelease.TagName,
                    StringComparison.OrdinalIgnoreCase
                )
                || latestRelease.TagName.Contains(
                    installedVersion,
                    StringComparison.OrdinalIgnoreCase
                );
        }
        catch (ApiException)
        {
            return false;
        }
    }

    public async Task<string> DownloadAsync(
        string targetPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var targetAssetName = GetPlatformAssetName();

        var latestRelease = await GitHubClient.Repository.Release.GetLatest(
            RepositoryOwner,
            RepositoryName
        );

        var archiveAsset =
            latestRelease.Assets.FirstOrDefault(a =>
                string.Equals(a.Name, targetAssetName, StringComparison.OrdinalIgnoreCase)
            )
            ?? throw new FileNotFoundException(
                $"Could not find matching release asset '{targetAssetName}' in the latest release."
            );

        var destinationDir = Directory.Exists(targetPath)
            ? targetPath
            : Path.GetDirectoryName(targetPath);

        if (string.IsNullOrEmpty(destinationDir))
        {
            throw new ArgumentException("Invalid executable path provided.", nameof(targetPath));
        }

        Directory.CreateDirectory(destinationDir);
        var tempZipPath = Path.Combine(
            Path.GetTempPath(),
            $"ffmpeg_{GuidGenerator.Create():N}.zip"
        );

        try
        {
            await DownloadFileAsync(
                HttpClient,
                archiveAsset.BrowserDownloadUrl,
                tempZipPath,
                progress,
                cancellationToken
            );

            await ZipFile.ExtractToDirectoryAsync(
                tempZipPath,
                destinationDir,
                overwriteFiles: true,
                cancellationToken: cancellationToken
            );

            CleanupUnusedBinaries(destinationDir);
            SetUnixExecutablePermissions(destinationDir);

            return latestRelease.TagName;
        }
        finally
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    private static void CleanupUnusedBinaries(string searchDirectory)
    {
        var filesToDelete = Directory.GetFiles(searchDirectory, "*", SearchOption.AllDirectories);
        foreach (var filePath in filesToDelete)
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            if (!string.Equals(fileNameWithoutExt, "ffprobe", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignored if locked or restricted
            }
        }
    }

    private static async Task<string?> GetInstalledFFmpegVersionAsync(
        string executablePath,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process process = new();
            process.StartInfo = startInfo;
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            var match = FFmpegVersionRegex().Match(output);
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetPlatformAssetName()
    {
        var os =
            OperatingSystem.IsWindows() ? "windows"
            : OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() ? "linux"
            : OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ? "osx"
            : OperatingSystem.IsAndroid() ? "android"
            : throw new PlatformNotSupportedException("Unsupported operating system.");

        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException(
                $"Architecture '{RuntimeInformation.ProcessArchitecture}' is not supported. Only 64-bit architectures are allowed."
            ),
        };

        return $"ffmpeg-{os}-{arch}.zip";
    }

    [GeneratedRegex(@"ffmpeg\s+version\s+([^\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FFmpegVersionRegex();
}
