using System.Diagnostics.CodeAnalysis;
using Gress;
using Rake.Core.Extensions;

namespace Rake.Core.Helpers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class BinariesHelper
{
    public static readonly string BinDir = EnvironmentHelper.AppDataDirectory.JoinPath("bin");
    public static readonly string YtDlpPluginsDir = BinDir.JoinPath("yt-dlp-plugins");
    public static string FFmpegFileName => GetFFmpegFileName();
    public static string YtDlpFileName => GetYtDlpFileName();
    public static string FFmpegPath { get; } =
        PathHelper.GetFromEnvironment(FFmpegFileName) ?? BinDir.JoinPath(FFmpegFileName);
    public static string YtDlpPath { get; } =
        PathHelper.GetFromEnvironment(YtDlpFileName) ?? BinDir.JoinPath(YtDlpFileName);
    public static bool FFmpegExist => File.Exists(FFmpegPath);
    public static bool YtDlpExist => File.Exists(YtDlpPath);
    public static bool BinariesExist => FFmpegExist && YtDlpExist;

    public static async Task<bool> DownloadBinaries(IProgress<Percentage>? progress = null)
    {
        var muxer = progress?.CreateMuxer();

        try
        {
            var tasks = new[]
            {
                DownloadFFmpegAsync(muxer?.CreateInput()),
                DownloadYtDlpAsync(muxer?.CreateInput()),
                DownloadYtDlpPluginsAsync(muxer?.CreateInput())
            };

            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public static async Task<bool> DownloadFFmpegAsync(IProgress<Percentage>? progress = null)
    {
        if (File.Exists(FFmpegPath))
        {
            progress?.Report(Percentage.FromFraction(1));
            return true;
        }

        Directory.CreateDirectory(BinDir);

        var zipName = $"ffmpeg-{OSHelper.GetPlatform().GetName().ToLower()}-x64.zip";
        var zipPath = BinDir.JoinPath(zipName);
        var downloadUrl = $"https://github.com/Tyrrrz/FFmpegBin/releases/latest/download/{zipName}";
        try
        {
            var muxer = progress?.CreateMuxer();
            var downloadProg = muxer?.CreateInput().ToDoubleBased();
            var zipProg = muxer?.CreateInput().ToDoubleBased();
            await downloadUrl.DownloadAsync(zipPath, downloadProg).ConfigureAwait(false);
            await ZipFileHelper.ExtractFileAsync(
                zipPath,
                FFmpegPath,
                FFmpegFileName,
                progress: zipProg
            );
            return true;
        }
        catch (Exception)
        {
            progress?.Report(Percentage.FromFraction(1));
            return false;
        }
        finally
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
    }

    public static async Task<bool> DownloadYtDlpAsync(IProgress<Percentage>? progress = null)
    {
        if (File.Exists(YtDlpPath))
        {
            progress?.Report(Percentage.FromFraction(1));
            return true;
        }

        Directory.CreateDirectory(BinDir);

        var downloadUrl =
            $"https://github.com/yt-dlp/yt-dlp/releases/latest/download/{YtDlpFileName}";

        try
        {
            await downloadUrl
                .DownloadAsync(YtDlpPath, progress?.ToDoubleBased())
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            progress?.Report(Percentage.FromFraction(1));
            return false;
        }
    }

    public static async Task<bool> DownloadYtDlpPluginsAsync(IProgress<Percentage>? progress = null)
    {
        var htvDir = YtDlpPluginsDir.JoinPath("htv");

        if (Directory.Exists(htvDir))
        {
            progress?.Report(Percentage.FromFraction(1));
            return true;
        }

        Directory.CreateDirectory(htvDir);

        var muxer = progress?.CreateMuxer();
        var htvZipPath = htvDir.JoinPath("plugin-htv.zip");

        try
        {
            await "https://github.com/cynthia2006/hanime-tv-plugin/archive/refs/heads/master.zip"
                .DownloadAsync(htvZipPath, muxer?.CreateInput().ToDoubleBased())
                .ConfigureAwait(false);

            await ZipFileHelper
                .ExtractDirectoryAsync(
                    htvZipPath,
                    htvDir,
                    "src",
                    progress: muxer?.CreateInput().ToDoubleBased()
                )
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (File.Exists(htvZipPath))
            {
                File.Delete(htvZipPath);
            }
        }

        return true;
    }

    private static string GetFFmpegFileName()
    {
        switch (OSHelper.GetPlatform())
        {
            case Platform.Windows:
                return "ffmpeg.exe";
            case Platform.OSX:
            case Platform.Linux:
                return "ffmpeg";
            case Platform.NotSupported:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static string GetYtDlpFileName()
    {
        switch (OSHelper.GetPlatform())
        {
            case Platform.Windows:
                return "yt-dlp.exe";
            case Platform.OSX:
                return "yt-dlp_macos";
            case Platform.Linux:
                return "yt-dlp";
            case Platform.NotSupported:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
