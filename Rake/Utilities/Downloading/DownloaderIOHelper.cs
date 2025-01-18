using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rake.Utilities.Downloading;

// ReSharper disable once InconsistentNaming
internal static class DownloaderIOHelper
{
    internal static async Task WriteStreamToFileChunkSessionAsync(
        ChunkSession session,
        DownloaderSpeedLimiter? downloadSpeedLimiter,
        int threadSize,
        HttpResponseInputStream? networkStream,
        bool isNetworkStreamFromExternal,
        Stream fileStream,
        DownloaderProgress downloaderProgress,
        DownloaderProgressDelegate? progressDelegateAsync,
        CancellationToken token
    )
    {
        long written = 0;
        long thisInstanceDownloadLimitBase = downloadSpeedLimiter?.InitialRequestedSpeed ?? -1;
        int currentRetry = 0;
        Stopwatch currentStopwatch = Stopwatch.StartNew();

        double maximumBytesPerSecond;
        double bitPerUnit;

        CalculateBps();

        StartWrite:
        byte[] buffer = ArrayPool<byte>.Shared.Rent(16 << 10);
        CancellationTokenSource? timeoutToken = null;
        CancellationTokenSource? coopToken = null;

        try
        {
            if (session.CurrentMetadata != null)
            {
                session.CurrentMetadata.UpdateChunkRangesCountEvent +=
                    CurrentMetadataUpdateChunkRangesCountEvent;
            }

            if (downloadSpeedLimiter != null)
            {
                downloadSpeedLimiter.DownloadSpeedChangedEvent +=
                    DownloaderDownloadSpeedLimitChanged;
            }

            if (
                session.CurrentPositions.End != 0
                && session.CurrentPositions.Start >= session.CurrentPositions.End
            )
            {
                return;
            }

            if (!isNetworkStreamFromExternal || (isNetworkStreamFromExternal && currentRetry > 0))
            {
                networkStream = await CreateStreamFromSessionAsync(session, token);
            }

            if (isNetworkStreamFromExternal && networkStream == null)
            {
                throw new NullReferenceException(
                    "networkStream argument cannot be null when isNetworkStreamFromExternal is set!"
                );
            }

            if (fileStream.CanSeek && session.CurrentPositions.End + 1 > fileStream.Length)
            {
                fileStream.SetLength(session.CurrentPositions.Start);
            }

            if (fileStream.CanSeek)
            {
                fileStream.Seek(session.CurrentPositions.Start, SeekOrigin.Begin);
            }

            timeoutToken = new CancellationTokenSource(session.TimeoutAfterInterval);
            coopToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, token);

            int read;
            while ((read = await networkStream!.ReadAsync(buffer, coopToken.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read, coopToken.Token);
                written += read;
                session.CurrentPositions.AdvanceStartOffset(read);
                session.CurrentMetadata?.UpdateLastEndOffset(session.CurrentPositions);
                downloaderProgress.AdvanceBytesDownloaded(read);
                progressDelegateAsync?.Invoke(read, downloaderProgress);

                timeoutToken.Dispose();
                coopToken.Dispose();

                timeoutToken = new CancellationTokenSource(session.TimeoutAfterInterval);
                coopToken = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutToken.Token,
                    token
                );

                await ThrottleAsync();

                currentRetry = 0;
            }
        }
        catch (TaskCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (NullReferenceException)
        {
            throw;
        }
        catch (Exception)
        {
            currentRetry++;
            if (currentRetry > session.RetryMaxAttempt)
            {
                throw;
            }

            await Task.Delay(session.RetryAttemptInterval, token);
            goto StartWrite;
        }
        finally
        {
            if (networkStream != null)
            {
                await networkStream.DisposeAsync();
            }

            ArrayPool<byte>.Shared.Return(buffer);

            if (downloadSpeedLimiter != null)
            {
                downloadSpeedLimiter.DownloadSpeedChangedEvent -=
                    DownloaderDownloadSpeedLimitChanged;
            }

            if (session.CurrentMetadata != null)
            {
                session.CurrentMetadata.UpdateChunkRangesCountEvent -=
                    CurrentMetadataUpdateChunkRangesCountEvent;
            }

            timeoutToken?.Dispose();
            coopToken?.Dispose();
        }

        return;

        void CalculateBps()
        {
            if (thisInstanceDownloadLimitBase <= 0)
                thisInstanceDownloadLimitBase = -1;
            else
                thisInstanceDownloadLimitBase = Math.Max(
                    Downloader.MinimumDownloadSpeedLimit,
                    thisInstanceDownloadLimitBase
                );

            double threadNum = Math.Min(
                (double)threadSize,
                session.CurrentMetadata?.Ranges?.Count ?? 2
            );
            maximumBytesPerSecond = thisInstanceDownloadLimitBase / threadNum;
            bitPerUnit = 940 - (threadNum - 2) / (16 - 2) * 400;
        }

        void DownloaderDownloadSpeedLimitChanged(object? sender, long e)
        {
            thisInstanceDownloadLimitBase = e == 0 ? -1 : e;
            CalculateBps();
        }

        void CurrentMetadataUpdateChunkRangesCountEvent(object? sender, bool e)
        {
            CalculateBps();
        }

        async Task ThrottleAsync()
        {
            // Make sure the buffer isn't empty.
            if (maximumBytesPerSecond <= 0 || written <= 0)
            {
                return;
            }

            long elapsedMilliseconds = currentStopwatch.ElapsedMilliseconds;

            if (elapsedMilliseconds > 0)
            {
                // Calculate the current bps.
                double bps = written * bitPerUnit / elapsedMilliseconds;

                // If the bps are more then the maximum bps, try to throttle.
                if (bps > maximumBytesPerSecond)
                {
                    // Calculate the time to sleep.
                    double wakeElapsed = written * bitPerUnit / maximumBytesPerSecond;
                    double toSleep = wakeElapsed - elapsedMilliseconds;

                    if (toSleep > 1)
                    {
                        // The time to sleep is more than a millisecond, so sleep.
                        await Task.Delay(TimeSpan.FromMilliseconds(toSleep), token);

                        // A sleep has been done, reset.
                        currentStopwatch.Restart();

                        written = 0;
                    }
                }
            }
        }
    }

    private static async ValueTask<HttpResponseInputStream?> CreateStreamFromSessionAsync(
        ChunkSession session,
        CancellationToken token
    )
    {
        // Assign the url and throw if null
        var fileUri = session.CurrentMetadata?.Url;
        if (fileUri == null)
        {
            throw new NullReferenceException(
                "Metadata was found to be null and it shouldn't happen!"
            );
        }

        // Create the network stream instance
        HttpResponseInputStream? stream = await HttpResponseInputStream.CreateStreamAsync(
            session.CurrentHttpClient,
            fileUri,
            session.CurrentPositions.Start,
            session.CurrentPositions.End,
            session.TimeoutAfterInterval,
            session.RetryAttemptInterval,
            session.RetryMaxAttempt,
            token
        );

        // Return the stream
        return stream;
    }
}
