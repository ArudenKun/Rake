using AutoInterfaceAttributes;
using Flurl;
using Gress;

namespace Rake.Core.Downloading;

public class VideoDownloader
{
    public Task DownloadAsync(
        Url url,
        IProgress<ICopyProgress>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }
}
