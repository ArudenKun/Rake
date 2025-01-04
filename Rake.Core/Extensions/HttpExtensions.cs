using Flurl;
using Gress;
using Rake.Core.Downloading;
using Rake.Core.Downloading.Internals;
using Rake.Core.Helpers;

namespace Rake.Core.Extensions;

public static class HttpExtensions
{
    public static HttpRequestMessage WithHeader(
        this HttpRequestMessage request,
        string name,
        string value
    )
    {
        request.Headers.Add(name, value);
        return request;
    }

    public static HttpRequestMessage WithHeaders(
        this HttpRequestMessage request,
        IDictionary<string, string?>? headers
    )
    {
        if (headers is null)
            return request;

        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return request;
    }

    public static async Task DownloadAsync(
        this HttpClient httpClient,
        Url url,
        Stream destinationStream,
        int bufferSize = 81920,
        IProgress<ICopyProgress>? progress = null,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(url, completionOption, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            progress?.Report(CopyProgress.Complete);
            return;
        }
        var totalLength = response.Content.Headers.ContentLength ?? 0L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await contentStream.CopyToAsync(
            destinationStream,
            bufferSize,
            totalLength,
            progress,
            cancellationToken
        );
    }

    public static async Task DownloadAsync(
        this HttpClient httpClient,
        Url url,
        string filePath,
        bool overwrite = true,
        int bufferSize = 81920,
        IProgress<ICopyProgress>? progress = null,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
        CancellationToken cancellationToken = default
    )
    {
        await using var fileStream = await IOHelper.OpenWriteAsync(filePath, overwrite, bufferSize);
        await DownloadAsync(
            httpClient,
            url,
            fileStream,
            bufferSize,
            progress,
            completionOption,
            cancellationToken
        );
    }

    /// <summary>
    /// Perform an HTTP GET with progress reporting capabilities.
    /// </summary>
    /// <param name="client">Extension variable.</param>
    /// <param name="requestUrl">The URI the request is sent to.</param>
    /// <param name="progress">A progress action which fires every time the write buffer is cycled.</param>
    /// <param name="cancelToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The full HTTP response. Reading from the response stream is discouraged.</returns>
    public static async Task<Stream> GetStreamAsync(
        this HttpClient client,
        Url requestUrl,
        IProgress<Percentage>? progress = null,
        CancellationToken cancelToken = default
    ) => await client.GetStreamAsync(requestUrl, progress.Wrap(), cancelToken);

    /// <summary>
    /// Perform an HTTP GET with progress reporting capabilities.
    /// </summary>
    /// <param name="client">Extension variable.</param>
    /// <param name="requestUrl">The URI the request is sent to.</param>
    /// <param name="progress">A progress action which fires every time the write buffer is cycled.</param>
    /// <param name="cancelToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The full HTTP response. Reading from the response stream is discouraged.</returns>
    public static async Task<Stream> GetStreamAsync(
        this HttpClient client,
        Url requestUrl,
        IProgress<ICopyProgress>? progress = null,
        CancellationToken cancelToken = default
    )
    {
        using var response = await client.GetAsync(
            requestUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancelToken
        );
        response.EnsureSuccessStatusCode();
        var contentLength = response.Content.Headers.ContentLength ?? 0;
        var responseStream = new MemoryStream();
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancelToken);
        await contentStream.CopyToAsync(
            responseStream,
            16384,
            contentLength,
            progress,
            cancelToken
        );

        if (responseStream.CanSeek)
        {
            responseStream.Position = 0;
        }

        return responseStream;
    }
}
