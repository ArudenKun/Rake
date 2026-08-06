using System.Diagnostics.CodeAnalysis;
using Rake.Core.Extensions;

namespace Rake;

public static class RakePathConsts
{
    public static readonly string Setting = RakeDirectoryConsts.Data.CombinePath(Name.Setting);
    public static readonly string Logs = RakeDirectoryConsts.Logs.CombinePath(Name.Logs);
    public static readonly string FFmpeg = RakeDirectoryConsts.Tools.CombinePath(Name.Ffmpeg);
    public static readonly string YtDlp = RakeDirectoryConsts.Tools.CombinePath(Name.YtDlp);
    public static readonly string Deno = RakeDirectoryConsts.Tools.CombinePath(Name.Deno);
    public static readonly string QuickJs = RakeDirectoryConsts.Tools.CombinePath(Name.QuickJs);
    public static readonly string Aria2 = RakeDirectoryConsts.Tools.CombinePath(Name.Aria2);

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Name
    {
        public const string Setting = "settings.json";
        public const string Logs = "logs.log";
#if WINDOWS
        public const string Ffmpeg = "ffmpeg.exe";
        public const string Deno = "deno.exe";
        public const string QuickJs = "qjs.exe";
        public const string YtDlp = "yt-dlp.exe";
        public const string Aria2 = "aria2c.exe";
#else
        public const string Ffmpeg = "ffmpeg";
        public const string Deno = "deno";
        public const string QuickJs = "qjs";
        public const string YtDlp = "yt-dlp";
        public const string Aria2 = "aria2c";
#endif
    }
}
