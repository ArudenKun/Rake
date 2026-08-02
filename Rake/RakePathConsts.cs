using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Rake.Core.Extensions;

namespace Rake;

public static class RakePathConsts
{
    public static readonly string Setting = RakeDirectoryConsts.Data.CombinePath(Name.Setting);
    public static readonly string Logs = RakeDirectoryConsts.Logs.CombinePath(Name.Logs);
    public static readonly string FFmpeg = RakeDirectoryConsts.Tools.CombinePath(Name.Ffmpeg);
    public static readonly string HandBrake =
#if !LINUX
    RakeDirectoryConsts.Tools.CombinePath(Name.HandBrake);
#else
    GetHandBrakeCliFromPath();
#endif
    public static readonly string YtDlp = RakeDirectoryConsts.Tools.CombinePath(Name.YtDlp);

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Name
    {
        public const string Setting = "settings.json";
        public const string Logs = "logs.log";
#if WINDOWS
        public const string Ffmpeg = "ffmpeg.exe";
        public const string HandBrake = "HandBrakeCLI.exe";
        public const string YtDlp = "yt-dlp.exe";
#else
        public const string Ffmpeg = "ffmpeg";
        public const string HandBrake = "HandBrakeCLI";
        public const string YtDlp = "yt-dlp";
#endif
    }

    private static string GetHandBrakeCliFromPath()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVariable))
        {
            foreach (var searchPath in pathVariable.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(searchPath, Name.HandBrake);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }
        return string.Empty;
    }
}
