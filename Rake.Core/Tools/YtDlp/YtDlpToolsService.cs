using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoInterfaceAttributes;
using Octokit;
using Volo.Abp.DependencyInjection;

namespace Rake.Core.Tools.YtDlp;

[AutoInterface]
internal class YtDlpToolsService : ToolsServiceBase, IYtDlpToolsService
{
    public YtDlpToolsService(IAbpLazyServiceProvider lazyServiceProvider, HttpClient httpClient)
        : base(lazyServiceProvider)
    {
        HttpClient = httpClient;
    }

    protected HttpClient HttpClient { get; }

    protected override string RepositoryOwner => "yt-dlp";
    protected override string RepositoryName => "yt-dlp";

    public bool IsAvailable(string targetPath) => IsAvailable(targetPath, "yt-dlp");

    public bool IsLocalAvailable(string targetPath) => IsLocalAvailable(targetPath, "yt-dlp");

    public async Task<string> GetVersionAsync(
        string targetPath,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetExecutablePath(targetPath, out var resolvedPath))
        {
            return string.Empty;
        }

        var version = await GetInstalledYtDlpVersionAsync(resolvedPath, cancellationToken);
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

        var installedVersion = await GetInstalledYtDlpVersionAsync(resolvedPath, cancellationToken);
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

        var asset =
            latestRelease.Assets.FirstOrDefault(a =>
                string.Equals(a.Name, targetAssetName, StringComparison.OrdinalIgnoreCase)
            )
            ?? throw new FileNotFoundException(
                $"Could not find matching release asset '{targetAssetName}' in the latest release."
            );

        var destinationFilePath = GetDestinationFilePath(targetPath);
        var destinationDir = Path.GetDirectoryName(destinationFilePath)!;

        Directory.CreateDirectory(destinationDir);
        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"yt-dlp_{GuidGenerator.Create():N}.tmp"
        );

        try
        {
            await DownloadFileAsync(
                HttpClient,
                asset.BrowserDownloadUrl,
                tempFilePath,
                progress,
                cancellationToken
            );

            File.Move(tempFilePath, destinationFilePath, overwrite: true);
            SetUnixExecutablePermissions(destinationFilePath);

            return latestRelease.TagName;
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    private static async Task<string?> GetInstalledYtDlpVersionAsync(
        string executablePath,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                Arguments = "--version",
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

            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string GetDestinationFilePath(string targetPath)
    {
        var defaultFileName = EnsureExecutableExtension("yt-dlp");

        if (Directory.Exists(targetPath) || targetPath.EndsWith('/') || targetPath.EndsWith('\\'))
        {
            return Path.Combine(targetPath, defaultFileName);
        }

        return string.IsNullOrEmpty(Path.GetExtension(targetPath)) && OperatingSystem.IsWindows()
            ? targetPath + ".exe"
            : targetPath;
    }

    private static string GetPlatformAssetName()
    {
        var arch = RuntimeInformation.ProcessArchitecture;

        if (OperatingSystem.IsWindows())
        {
            return arch switch
            {
                Architecture.X64 => "yt-dlp.exe",
                _ => throw new PlatformNotSupportedException(
                    $"Architecture '{arch}' is not supported on Windows."
                ),
            };
        }

        if (OperatingSystem.IsLinux())
        {
            return "yt-dlp";
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return "yt-dlp_macos";
        }

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }
}
