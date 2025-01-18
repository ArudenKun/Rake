using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Flurl;

namespace Rake.Utilities.Downloading;

internal static class Extensions
{
    internal static async ValueTask<long> GetUrlContentLengthAsync(
        this Url url,
        HttpClient client,
        int retryCount,
        TimeSpan retryInterval,
        TimeSpan timeoutInterval,
        CancellationToken token
    )
    {
        var currentRetry = 0;
        Start:
        var request = new HttpRequestMessage { RequestUri = url.ToUri() };
        HttpResponseMessage? message = null;

        var cancelTimeoutToken = new CancellationTokenSource(timeoutInterval);
        var coopToken = CancellationTokenSource.CreateLinkedTokenSource(
            cancelTimeoutToken.Token,
            token
        );

        try
        {
            request.Headers.Range = new RangeHeaderValue(0, null);
            message = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                coopToken.Token
            );
            return message.Content.Headers.ContentLength ?? 0;
        }
        catch (TaskCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            currentRetry++;
            if (currentRetry > retryCount)
                throw;

            await Task.Delay(retryInterval, token);
            goto Start;
        }
        finally
        {
            request.Dispose();
            message?.Dispose();
            cancelTimeoutToken.Dispose();
            coopToken.Dispose();
        }
    }

    internal static bool CanSeeLength(this Stream stream)
    {
        try
        {
            _ = stream.Length;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
