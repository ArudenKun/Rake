using Microsoft.Extensions.Logging;
using Octokit;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using FileMode = System.IO.FileMode;

namespace Rake.Core.Tools;

internal abstract class ToolsServiceBase
{
    protected ToolsServiceBase(IAbpLazyServiceProvider lazyServiceProvider)
    {
        LazyServiceProvider = lazyServiceProvider;
    }

    protected abstract string RepositoryOwner { get; }
    protected abstract string RepositoryName { get; }

    protected IAbpLazyServiceProvider LazyServiceProvider { get; }

    protected ILoggerFactory LoggerFactory =>
        LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        LazyServiceProvider.LazyGetService<ILogger>(_ =>
            LoggerFactory.CreateLogger(GetType().FullName!)
        );

    protected IClock Clock => LazyServiceProvider.LazyGetRequiredService<IClock>();

    protected IGuidGenerator GuidGenerator =>
        LazyServiceProvider.LazyGetRequiredService<IGuidGenerator>();

    protected IGitHubClient GitHubClient =>
        LazyServiceProvider.LazyGetRequiredService<IGitHubClient>();

    protected IReleasesClient ReleasesClient =>
        LazyServiceProvider.LazyGetRequiredService<IReleasesClient>();

    public virtual bool IsAvailable(string targetPath, string defaultBinaryName)
    {
        return IsLocalAvailable(targetPath, defaultBinaryName) || IsPathAvailable(targetPath);
    }

    public virtual bool IsPathAvailable(string targetPath)
    {
        var fileName = EnsureExecutableExtension(Path.GetFileName(targetPath));
        var pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathVariable))
            return false;

        return pathVariable
            .Split(Path.PathSeparator)
            .Any(searchPath => File.Exists(Path.Combine(searchPath, fileName)));
    }

    public virtual bool IsLocalAvailable(string targetPath, string defaultBinaryName)
    {
        if (File.Exists(targetPath))
            return true;

        var baseDir = Directory.Exists(targetPath)
            ? targetPath
            : Path.GetDirectoryName(targetPath) ?? string.Empty;

        if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir))
            return false;

        var binaryName = EnsureExecutableExtension(defaultBinaryName);
        return File.Exists(Path.Combine(baseDir, binaryName));
    }

    protected static bool TryGetExecutablePath(string targetPath, out string resolvedPath)
    {
        if (File.Exists(targetPath))
        {
            resolvedPath = targetPath;
            return true;
        }

        var fileName = EnsureExecutableExtension(Path.GetFileName(targetPath));
        var pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (!string.IsNullOrEmpty(pathVariable))
        {
            foreach (var searchPath in pathVariable.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(searchPath, fileName);
                if (File.Exists(fullPath))
                {
                    resolvedPath = fullPath;
                    return true;
                }
            }
        }

        resolvedPath = string.Empty;
        return false;
    }

    protected static string EnsureExecutableExtension(string fileName)
    {
        if (
            OperatingSystem.IsWindows()
            && !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
        )
        {
            return fileName + ".exe";
        }
        return fileName;
    }

    protected static async Task DownloadFileAsync(
        HttpClient httpClient,
        string downloadUrl,
        string destinationFilePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken
    )
    {
        using var response = await httpClient.GetAsync(
            downloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8192,
            useAsync: true
        );

        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;

            if (totalBytes is > 0)
            {
                progress?.Report((double)totalBytesRead / totalBytes.Value * 100.0);
            }
        }
    }

    protected static void SetUnixExecutablePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
            return;

        var files =
            Directory.Exists(path) ? Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            : File.Exists(path) ? new[] { path }
            : Array.Empty<string>();

        const UnixFileMode executablePermissions =
            UnixFileMode.UserRead
            | UnixFileMode.UserWrite
            | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead
            | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead
            | UnixFileMode.OtherExecute;

        foreach (var file in files)
        {
            try
            {
                File.SetUnixFileMode(file, executablePermissions);
            }
            catch
            {
                // Ignored if unsupported on file system
            }
        }
    }
}
