using Rake.Core.YtDlp;
using Rake.Core.YtDlp.Enums;

namespace Rake;

public static class YtDlpExtensions
{
    public static YtDlp WithDefaults(this YtDlp ytDlp) =>
        ytDlp
            .WithNoJsRuntime()
            .WithJsRuntime(Runtime.QuickJs, RakePathConsts.QuickJs)
            .WithRemoteComponent("ejs:github")
            .WithFFmpegLocation(RakePathConsts.FFmpeg);
}
