using Flurl.Http;
using Rake.Core.Helpers;

namespace Rake.Core.Extensions;

public static class FlurlExtensions
{
    public static async Task DownloadAsync(
        this string url,
        string filePath,
        IProgress<double>? progress = null,
        int bufferSize = 81920,
        bool overwrite = true,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
        CancellationToken cancellationToken = default
    )
    {
        var response = await url.GetAsync(completionOption, cancellationToken);

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            return;
        }

        var totalLength = response.ResponseMessage.Content.Headers.ContentLength ?? 0L;
        var contentStream = await response.ResponseMessage.Content.ReadAsStreamAsync(
            cancellationToken
        );

        var fileStream = await IOHelper.OpenWriteAsync(filePath, overwrite, bufferSize);

        try
        {
            await contentStream.CopyToAsync(
                fileStream,
                totalLength,
                bufferSize,
                progress,
                cancellationToken
            );
        }
        finally
        {
            fileStream.Close();
            contentStream.Close();
        }
    }
}
