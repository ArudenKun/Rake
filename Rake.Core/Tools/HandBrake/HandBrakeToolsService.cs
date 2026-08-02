using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AutoInterfaceAttributes;
using Octokit;
using Volo.Abp.DependencyInjection;

namespace Rake.Core.Tools.HandBrake;

[AutoInterface]
internal partial class HandBrakeToolsService : ToolsServiceBase, IHandBrakeToolsService
{
    public HandBrakeToolsService(IAbpLazyServiceProvider lazyServiceProvider, HttpClient httpClient)
        : base(lazyServiceProvider)
    {
        HttpClient = httpClient;
    }

    protected HttpClient HttpClient { get; }

    protected override string RepositoryOwner => "HandBrake";
    protected override string RepositoryName => "HandBrake";

    public bool IsAvailable(string targetPath) => IsAvailable(targetPath, "HandBrakeCLI");

    public bool IsLocalAvailable(string targetPath) => IsLocalAvailable(targetPath, "HandBrakeCLI");

    public async Task<string> GetVersionAsync(
        string targetPath,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetExecutablePath(targetPath, out var resolvedPath))
        {
            return string.Empty;
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = resolvedPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process process = new();
            process.StartInfo = startInfo;
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(cancellationToken);

            var combinedOutput = $"{stdoutTask.Result}\n{stderrTask.Result}";
            if (string.IsNullOrWhiteSpace(combinedOutput))
            {
                return string.Empty;
            }

            var match = HandBrakeVersionRegex().Match(combinedOutput);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
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

        var installedVersion = await GetVersionAsync(resolvedPath, cancellationToken);
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
        var latestRelease = await GitHubClient.Repository.Release.GetLatest(
            RepositoryOwner,
            RepositoryName
        );

        var asset =
            latestRelease.Assets.FirstOrDefault(a => IsMatchingPlatformAsset(a.Name))
            ?? throw new FileNotFoundException(
                "Could not find a matching HandBrakeCLI release asset for this platform."
            );

        var destinationDir = Directory.Exists(targetPath)
            ? targetPath
            : Path.GetDirectoryName(targetPath);

        if (string.IsNullOrEmpty(destinationDir))
        {
            throw new ArgumentException("Invalid path provided.", nameof(targetPath));
        }

        Directory.CreateDirectory(destinationDir);
        var tempZipPath = Path.Combine(
            Path.GetTempPath(),
            $"handbrake_{GuidGenerator.Create():N}.zip"
        );

        try
        {
            await DownloadFileAsync(
                HttpClient,
                asset.BrowserDownloadUrl,
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

            SetUnixExecutablePermissions(destinationDir);
            return latestRelease.TagName;
        }
        finally
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    private static bool IsMatchingPlatformAsset(string assetName)
    {
        var arch = RuntimeInformation.ProcessArchitecture;

        if (OperatingSystem.IsWindows())
        {
            return assetName.Contains("HandBrakeCLI", StringComparison.OrdinalIgnoreCase)
                && assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                && (
                    (
                        arch == Architecture.X64
                        && assetName.Contains("x86_64", StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        arch == Architecture.Arm64
                        && assetName.Contains("arm64", StringComparison.OrdinalIgnoreCase)
                    )
                );
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return assetName.Contains("HandBrakeCLI", StringComparison.OrdinalIgnoreCase)
                && assetName.EndsWith(".dmg", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    [GeneratedRegex(@"HandBrake(?:CLI)?\s+([0-9]+\.[0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex HandBrakeVersionRegex();
}
