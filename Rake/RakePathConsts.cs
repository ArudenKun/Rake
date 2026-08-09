using System.Diagnostics.CodeAnalysis;
using Rake.Core.Extensions;

namespace Rake;

public static class RakePathConsts
{
    public static readonly string Setting = RakeDirectoryConsts.Data.CombinePath(Name.Setting);
    public static readonly string Logs = RakeDirectoryConsts.Logs.CombinePath(Name.Logs);
    public static readonly string FFmpeg = RakeDirectoryConsts.Tools.CombinePath(Name.FFmpeg);
    public static readonly string TwitchDownloaderCli = RakeDirectoryConsts.Tools.CombinePath(
        Name.TwitchDownloaderCli
    );

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Name
    {
        public const string Setting = "settings.json";
        public const string Logs = "logs.log";
#if WINDOWS
        public const string FFmpeg = "ffmpeg.exe";
        public const string TwitchDownloaderCli = "TwitchDownloaderCLI.exe";
#else
        public const string FFmpeg = "ffmpeg";
        public const string TwitchDownloaderCli = "TwitchDownloaderCLI";
#endif
    }
}
