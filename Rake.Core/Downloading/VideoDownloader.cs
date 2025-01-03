using AutoInterfaceAttributes;
using Flurl;
using Gress;

namespace Rake.Core.Downloading;

[AutoInterface]
public class VideoDownloader : IVideoDownloader
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
