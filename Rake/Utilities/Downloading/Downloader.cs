﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Rake.Helpers;

namespace Rake.Utilities.Downloading;

/// <summary>
///     An instance of the downloader. This instance can be re-used. But besides it can be re-used, make sure to set
///     the sufficient <c>MaxConnection</c> of the <seealso cref="HttpClient" />
///     assigned into the current <seealso cref="Downloader" /> instance.
/// </summary>
public sealed class Downloader
{
    private const int DefaultConnectionSessions = 4;
    private const int DefaultRetryCountMax = 5;
    private const int DefaultSessionChunkSize = 4 << 20; // 4 MiB for each chunk size
    internal const int MinimumDownloadSpeedLimit = 256 << 10; // 262144 bytes/s (256 KiB/s)

    private Downloader(
        HttpClient httpClient,
        int retryCountMax = DefaultRetryCountMax,
        TimeSpan? retryAttemptInterval = null,
        TimeSpan? timeoutAfterInterval = null
    )
    {
        retryAttemptInterval ??= TimeSpan.FromSeconds(1);
        RetryAttemptInterval = retryAttemptInterval.Value;

        timeoutAfterInterval ??= TimeSpan.FromSeconds(10);
        TimeoutAfterInterval = timeoutAfterInterval.Value;

        RetryCountMax = retryCountMax;
        CurrentHttpClientInstance = httpClient;
    }

    private TimeSpan RetryAttemptInterval { get; }
    private int RetryCountMax { get; }
    private TimeSpan TimeoutAfterInterval { get; }
    private HttpClient CurrentHttpClientInstance { get; }

    /// <summary>
    ///     Create an instance of a Http Download Client instance from the given <seealso cref="HttpClient" /> instance.
    /// </summary>
    /// <param name="httpClient">
    ///     Use the HttpClient from the parent caller from the given <seealso cref="HttpClient" /> instance.
    /// </param>
    /// <param name="retryCountMax">
    ///     Count of how many times the retry attempt should be accepted.
    /// </param>
    /// <param name="retryAttemptInterval">
    ///     Determine how long the pause will run before the next retry attempt is executed. The default value is 1 second.
    /// </param>
    /// <param name="timeoutAfterInterval">
    ///     Determine how long the method will time out while it's getting called.
    /// </param>
    /// <returns>An instance of a Http Download Client</returns>
    /// <exception cref="NullReferenceException">Throw if the <paramref name="httpClient" /> argument is <c>null</c>.</exception>
    public static Downloader CreateInstance(
        HttpClient? httpClient = null,
        int retryCountMax = DefaultRetryCountMax,
        TimeSpan? retryAttemptInterval = null,
        TimeSpan? timeoutAfterInterval = null
    )
    {
        httpClient ??= HttpHelper.HttpClient;
        // Return the instance
        return new Downloader(
            httpClient,
            retryCountMax,
            retryAttemptInterval,
            timeoutAfterInterval
        );
    }

    /// <summary>
    ///     Start the download process of the file asynchronously. This method can be used for single-session or multi-session
    ///     download.
    /// </summary>
    /// <param name="url">The URL of the file to be downloaded.</param>
    /// <param name="fileOutputPath">
    ///     Output path of the file to download.
    /// </param>
    /// <param name="useOverwrite">
    ///     Overwrite the download even the previous session can be continued.
    ///     If set to true, the previous download will start from beginning.
    /// </param>
    /// <param name="progressDelegateAsync">
    ///     The delegate callback to process the download progress information.<br /><br />
    ///     Default: <c>null</c>
    /// </param>
    /// <param name="downloaderSpeedLimiter">
    ///     If the download speed limiter is null, the download speed will be set to unlimited.
    /// </param>
    /// <param name="maxConnectionSessions">
    ///     How much connection session to be started for the download process. If it's being set to less than or equal as 0,
    ///     then it will fall back to the default value: 4.<br /><br />
    ///     Default: <c>4</c>
    /// </param>
    /// <param name="sessionChunkSize">
    ///     How big the size of each session chunk.<br /><br />
    ///     Default: <c>4,194,304 bytes</c> or <c>4 MiB</c>
    /// </param>
    /// <param name="cancelToken">
    ///     Cancellation token. If not assigned, a cancellation token will not be assigned and the download becomes
    ///     non-cancellable.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="url" /> or <paramref name="fileOutputPath" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="url" /> or <paramref name="fileOutputPath" /> is empty or only have
    ///     whitespaces.
    /// </exception>
    public async Task DownloadAsync(
        Url url,
        string fileOutputPath,
        bool useOverwrite = false,
        DownloaderProgressDelegate? progressDelegateAsync = null,
        int maxConnectionSessions = DefaultConnectionSessions,
        int sessionChunkSize = DefaultSessionChunkSize,
        DownloaderSpeedLimiter? downloaderSpeedLimiter = null,
        CancellationToken cancelToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(url, nameof(url));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        ArgumentException.ThrowIfNullOrEmpty(fileOutputPath, nameof(fileOutputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileOutputPath, nameof(fileOutputPath));

        var fileStreamOptions = new FileStreamOptions
        {
            Mode = FileMode.OpenOrCreate,
            Access = FileAccess.Write,
            Share = FileShare.ReadWrite,
            Options = FileOptions.WriteThrough,
        };

        var downloadProgressStruct = new DownloaderProgress();

        try
        {
            await Parallel.ForEachAsync(
                ChunkSession.EnumerateMultipleChunks(
                    CurrentHttpClientInstance,
                    url,
                    fileOutputPath,
                    useOverwrite,
                    sessionChunkSize,
                    downloadProgressStruct,
                    progressDelegateAsync,
                    RetryCountMax,
                    RetryAttemptInterval,
                    TimeoutAfterInterval,
                    cancelToken
                ),
                new ParallelOptions
                {
                    CancellationToken = cancelToken,
                    MaxDegreeOfParallelism = maxConnectionSessions,
                },
                async (chunk, coopCancelToken) =>
                    await PerformDownloadWriteDelegate(
                        progressDelegateAsync,
                        maxConnectionSessions,
                        downloaderSpeedLimiter,
                        chunk,
                        fileStreamOptions,
                        downloadProgressStruct,
                        coopCancelToken
                    )
            );
        }
        catch (AggregateException ex)
        {
            throw ex.Flatten().InnerExceptions.First();
        }

        Metadata.DeleteMetadataFile(fileOutputPath);
    }

    private static async Task PerformDownloadWriteDelegate(
        DownloaderProgressDelegate? progressDelegateAsync,
        int maxConnectionSessions,
        DownloaderSpeedLimiter? downloaderSpeedLimiter,
        ChunkSession chunk,
        FileStreamOptions fileStreamOptions,
        DownloaderProgress downloaderProgressStruct,
        CancellationToken cancelToken
    )
    {
        await using var stream = new FileStream(
            chunk.CurrentMetadata?.OutputFilePath!,
            fileStreamOptions
        );
        if (chunk.CurrentMetadata == null)
        {
            throw new NullReferenceException("chunk.CurrentMetadata reference is null");
        }

        await chunk.CurrentMetadata.SaveLastMetadataStateAsync(cancelToken);
        await DownloaderIOHelper.WriteStreamToFileChunkSessionAsync(
            chunk,
            downloaderSpeedLimiter,
            maxConnectionSessions,
            null,
            false,
            stream,
            downloaderProgressStruct,
            progressDelegateAsync,
            cancelToken
        );

        if (chunk.CurrentPositions.End - 1 > chunk.CurrentPositions.Start)
            throw new Exception();

        chunk.CurrentMetadata.PopRange(chunk.CurrentPositions);
        await chunk.CurrentMetadata.SaveLastMetadataStateAsync(cancelToken);
    }

    /// <summary>
    ///     Start the download process to a <seealso cref="Stream" />.
    /// </summary>
    /// <param name="url">The URL of the file to be downloaded into stream.</param>
    /// <param name="outputStream">
    ///     The output stream where the data will be downloaded. The <paramref name="outputStream" /> must be writable.
    /// </param>
    /// <param name="allowContinue">
    ///     Allow to resume the last position of the download.
    ///     However, this argument will be ignored if the <paramref name="outputStream" /> is not seekable and
    ///     <paramref name="offsetStart" /> is set to > 0.
    /// </param>
    /// <param name="offsetStart">
    ///     The start position of the data to be downloaded. If this argument is set with <paramref name="offsetEnd" /> to
    ///     <c>null</c>,<br />
    ///     the download will start from the beginning of the data. The <paramref name="offsetStart" /> argument cannot be set
    ///     more than or equal as <paramref name="offsetEnd" />.<br /><br />
    ///     Default: <c>null</c>
    /// </param>
    /// <param name="offsetEnd">
    ///     The end position of the data to be downloaded. If this argument is set to <c>null</c>,<br />
    ///     the download will write the data until the end of the data. The <paramref name="offsetEnd" /> argument cannot be
    ///     set less than or equal as <paramref name="offsetStart" />.<br /><br />
    ///     Default: <c>null</c>
    /// </param>
    /// <param name="progressDelegateAsync">
    ///     The delegate callback to process the download progress information.<br /><br />
    ///     Default: <c>null</c>
    /// </param>
    /// <param name="downloaderSpeedLimiter">
    ///     If the download speed limiter is null, the download speed will be set to unlimited.
    /// </param>
    /// <param name="cancelToken">
    ///     Cancellation token. If not assigned, a cancellation token will not be assigned and the download becomes
    ///     non-cancellable.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="url" /> or <paramref name="outputStream" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="url" /> is empty or only have whitespaces.</exception>
    public async Task DownloadAsync(
        string url,
        Stream outputStream,
        bool allowContinue,
        DownloaderProgressDelegate? progressDelegateAsync = null,
        long? offsetStart = null,
        long? offsetEnd = null,
        DownloaderSpeedLimiter? downloaderSpeedLimiter = null,
        CancellationToken cancelToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(url, nameof(url));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        ArgumentNullException.ThrowIfNull(outputStream, nameof(outputStream));

        offsetStart ??= 0;

        var isSeeked = false;

        // If the download allows the download to continue and the outputStream is seekable,
        // then seek the outputStream to the very end of its position.
        if (
            allowContinue
            && outputStream.CanSeek
            && outputStream.CanSeeLength()
            && offsetStart == 0
        )
        {
            outputStream.Seek(0, SeekOrigin.End);
            isSeeked = true;
        }

        // Create the session and stream tuple
        var networkStream = await ChunkSession.CreateSingleSessionAsync(
            CurrentHttpClientInstance,
            url,
            offsetStart,
            offsetEnd,
            RetryCountMax,
            RetryAttemptInterval,
            TimeoutAfterInterval,
            cancelToken
        );

        // If the network stream tuple is null, then ignore
        if (networkStream == null)
        {
            return;
        }

        // Set the download progress struct and set the bytes total to download.
        var downloadProgressStruct = new DownloaderProgress();
        downloadProgressStruct.SetBytesTotal(networkStream.Value.Item2.Length);

        // If the outputStream is seekable, then advance the downloaded progress.
        if (isSeeked)
        {
            downloadProgressStruct.AdvanceBytesDownloaded(outputStream.Length);
        }

        // Start the download
        await DownloaderIOHelper.WriteStreamToFileChunkSessionAsync(
            networkStream.Value.Item1,
            downloaderSpeedLimiter,
            1,
            networkStream.Value.Item2,
            true,
            outputStream,
            downloadProgressStruct,
            progressDelegateAsync,
            cancelToken
        );
    }

    /// <summary>
    /// Get the current <seealso cref="HttpClient"/> instance used by the <seealso cref="Downloader"/>
    /// </summary>
    /// <returns>The current <seealso cref="HttpClient"/> instance used by the <seealso cref="Downloader"/></returns>
    public HttpClient GetHttpClient() => CurrentHttpClientInstance;

    /// <summary>
    /// Get the Http's <seealso cref="HttpResponseMessage.StatusCode"/> of the URL.
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <param name="cancelToken">The cancellation token</param>
    /// <returns>A tuple contains a <seealso cref="HttpResponseMessage.StatusCode"/> and an <seealso cref="bool"/> of the status code (true = success, false = failed)</returns>
    public async ValueTask<(HttpStatusCode, bool)> GetUrlStatus(
        string url,
        CancellationToken cancelToken
    ) => await GetUrlStatus(CurrentHttpClientInstance, url, cancelToken);

    /// <summary>
    /// Get the Http's <seealso cref="HttpResponseMessage.StatusCode"/> of the URL.
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <param name="cancelToken">The cancellation token</param>
    /// <returns>A tuple contains a <seealso cref="HttpResponseMessage.StatusCode"/> and an <seealso cref="bool"/> of the status code (true = success, false = failed)</returns>
    public async ValueTask<(HttpStatusCode, bool)> GetUrlStatus(
        Url url,
        CancellationToken cancelToken
    ) => await GetUrlStatus(CurrentHttpClientInstance, url, cancelToken);

    /// <summary>
    /// Get the Http's <seealso cref="HttpResponseMessage.StatusCode"/> of the URL from an <seealso cref="HttpClient"/> instance.
    /// </summary>
    /// <param name="httpClient">The <seealso cref="HttpClient"/> instance to be used for URL checking</param>
    /// <param name="url">The URL to check</param>
    /// <param name="cancelToken">The cancellation token</param>
    /// <returns>A tuple contains a <seealso cref="HttpResponseMessage.StatusCode"/> and an <seealso cref="bool"/> of the status code (true = success, false = failed)</returns>
    public static async ValueTask<(HttpStatusCode, bool)> GetUrlStatus(
        HttpClient httpClient,
        Url url,
        CancellationToken cancelToken
    )
    {
        using var response = await httpClient.SendAsync(
            new HttpRequestMessage { RequestUri = url.ToUri() },
            HttpCompletionOption.ResponseHeadersRead,
            cancelToken
        );
        return (response.StatusCode, response.IsSuccessStatusCode);
    }

    /// <summary>
    /// Get the total size of an existing downloaded file using the current instance of a <seealso cref="Downloader"/>.
    /// </summary>
    /// <param name="fileUrl">The URL of the file.</param>
    /// <param name="filePath">The path of the existing download file.</param>
    /// <param name="expectedLength">The expected value of the total size. If it's set to 0, then a GET HTTP routine will not be requested.</param>
    /// <param name="cancelToken">The cancellation token for the routine.</param>
    /// <returns>A size of a downloaded file.</returns>
    /// <exception cref="InvalidOperationException">If <paramref name="expectedLength"/> is set to 0 and the current <seealso cref="HttpClient"/> of this <seealso cref="Downloader"/> is null.</exception>
    public async ValueTask<long> GetDownloadedFileSize(
        string fileUrl,
        string filePath,
        long expectedLength = 0,
        CancellationToken cancelToken = default
    ) =>
        await GetDownloadedFileSize(
            fileUrl,
            filePath,
            CurrentHttpClientInstance,
            expectedLength,
            RetryCountMax,
            RetryAttemptInterval,
            TimeoutAfterInterval,
            cancelToken
        );

    /// <summary>
    /// Get the total size of an existing downloaded file using the current instance of a <seealso cref="Downloader"/>.
    /// </summary>
    /// <param name="fileUrl">The URL of the file.</param>
    /// <param name="filePath">The path of the existing download file.</param>
    /// <param name="expectedLength">The expected value of the total size. If it's set to 0, then a GET HTTP routine will not be requested.</param>
    /// <param name="cancelToken">The cancellation token for the routine.</param>
    /// <returns>A size of a downloaded file.</returns>
    /// <exception cref="InvalidOperationException">If <paramref name="expectedLength"/> is set to 0 and the current <seealso cref="HttpClient"/> of this <seealso cref="Downloader"/> is null.</exception>
    public async ValueTask<long> GetDownloadedFileSize(
        Url fileUrl,
        string filePath,
        long expectedLength = 0,
        CancellationToken cancelToken = default
    ) =>
        await GetDownloadedFileSize(
            fileUrl,
            filePath,
            CurrentHttpClientInstance,
            expectedLength,
            RetryCountMax,
            RetryAttemptInterval,
            TimeoutAfterInterval,
            cancelToken
        );

    /// <summary>
    /// Get the total size of an existing downloaded file
    /// </summary>
    /// <param name="fileUrl">The URL of the file.</param>
    /// <param name="httpClient"><seealso cref="HttpClient"/> instance to be used for checking the total size of the file if <paramref name="expectedLength"/> is 0.</param>
    /// <param name="filePath">The path of the existing download file.</param>
    /// <param name="expectedLength">The expected value of the total size. If it's set to 0, then a GET HTTP routine will not be requested.</param>
    /// <param name="retryCountMax">An expected amount of time for retrying the check.</param>
    /// <param name="retryAttemptInterval">How much time for delay to run before the next retry attempt.</param>
    /// <param name="timeoutAfterInterval">How much time for a timeout to trigger for retrying a routine.</param>
    /// <param name="cancelToken">The cancellation token for the routine.</param>
    /// <returns>A size of a downloaded file.</returns>
    /// <exception cref="InvalidOperationException">If <paramref name="httpClient"/> is null and <paramref name="expectedLength"/> is set to 0</exception>
    public static async ValueTask<long> GetDownloadedFileSize(
        Url fileUrl,
        string filePath,
        HttpClient? httpClient = null,
        long expectedLength = 0,
        int? retryCountMax = null,
        TimeSpan? retryAttemptInterval = null,
        TimeSpan? timeoutAfterInterval = null,
        CancellationToken cancelToken = default
    )
    {
        // Set default values
        retryCountMax ??= DefaultRetryCountMax;
        retryAttemptInterval ??= TimeSpan.FromSeconds(1);
        timeoutAfterInterval ??= TimeSpan.FromSeconds(10);

        // Sanity check: Throw if httpClient is null while expectedLength param is 0
        // if (httpClient == null && expectedLength == 0)
        //     throw new InvalidOperationException(
        //         $"You cannot set {nameof(httpClient)} to null while {nameof(expectedLength)} is set to 0!"
        //     );

        httpClient ??= HttpHelper.HttpClient;

        // Get the file size from the URL or expected value
        var contentLength =
            expectedLength > 0
                ? expectedLength
                : await fileUrl.GetUrlContentLengthAsync(
                    httpClient,
                    (int)retryCountMax,
                    retryAttemptInterval.Value,
                    timeoutAfterInterval.Value,
                    cancelToken
                );

        // Get current file info
        var currentFileInfo = new FileInfo(filePath);

        // Get metadata file info
        var metadataFilePath = currentFileInfo.FullName + Metadata.MetadataExtension;
        var metadataFileInfo = new FileInfo(metadataFilePath);

        // Get the last session metadata info
        var currentSessionMetadata = await Metadata.ReadLastMetadataAsync(
            null,
            currentFileInfo,
            metadataFileInfo,
            0,
            cancelToken
        );

        // Set the current length to 0;
        long currentLength = 0;

        // SANITY CHECK: Metadata and file state check
        if (
            (metadataFileInfo.Exists && metadataFileInfo.Length < 64 && currentFileInfo.Exists)
            || (currentFileInfo.Exists && currentFileInfo.Length > contentLength)
            || (metadataFileInfo.Exists && !currentFileInfo.Exists)
            || (
                (currentSessionMetadata?.Ranges?.Count ?? 0) == 0
                && (currentSessionMetadata?.IsCompleted ?? false)
            )
        )
        {
            // Return 0 as uncompleted
            return 0;
        }

        // If the completed flag is set, the ranges are empty, the output file exist with the length is equal,
        // then return from enumerating. Or if the ranges list is empty, return
        if (
            (
                currentSessionMetadata?.Ranges?.Count == 0
                && currentFileInfo.Exists
                && currentFileInfo.Length == contentLength
            )
            || currentSessionMetadata?.Ranges == null
        )
        {
            currentLength += contentLength;
            return currentLength;
        }

        // Enumerate last ranges
        // long lastEndOffset = currentSessionMetadata.Ranges.Count > 0
        //     ? currentSessionMetadata.Ranges.Max(x => x?.End ?? 0) + 1
        //     : 0;

        // If the metadata is not exist, but it has an uncompleted file with size > DefaultSessionChunkSize,
        // then try to resume the download and advance the lastEndOffset from the file last position.
        if (
            currentSessionMetadata.Ranges.Count == 0
            && currentFileInfo.Exists
            && currentSessionMetadata.LastEndOffset <= currentFileInfo.Length
        )
        {
            currentLength += currentFileInfo.Length;
        }
        // Else if the file exist with size downloaded less than LastEndOffset, then continue
        // the position based on metadata.
        else if (currentFileInfo.Exists)
        {
            var lastRange = new ChunkRange();
            foreach (var range in currentSessionMetadata.Ranges)
            {
                if (range == null)
                {
                    continue;
                }

                var toAdd = range.Start - lastRange.End;
                currentLength += toAdd;

                lastRange = range;
            }

            return currentLength;
        }

        // Otherwise, return currentLength
        return currentLength;
    }
}
