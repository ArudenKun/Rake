using Rake.Core.YtDlp;

namespace Rake;

public static class YtDlpExtensions
{
    public static YtDlp WithDefaults(this YtDlp ytDlp) =>
        ytDlp.WithRemoteComponent("ejs:github").WithFFmpegLocation(RakePathConsts.FFmpeg);
}
