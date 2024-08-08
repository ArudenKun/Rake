using System;
using System.IO;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Rake.Services.Caching;

public sealed class FileCacheImageLoader : IAsyncImageLoader
{
    private readonly IFusionCache _fusionCache;
    private readonly ILogger<FileCacheImageLoader> _logger;

    public FileCacheImageLoader(IFusionCache fusionCache, ILogger<FileCacheImageLoader> logger)
    {
        _fusionCache = fusionCache;
        _logger = logger;
    }

    public async Task<Bitmap?> ProvideImageAsync(string url)
    {
        var internalOrLocalBitmap =
            await LoadFromInternalAsync(url).ConfigureAwait(false)
            ?? await LoadFromLocalAsync(url).ConfigureAwait(false);

        if (internalOrLocalBitmap != null)
            return internalOrLocalBitmap;

        try
        {
            var externalBytes = await _fusionCache
                .GetOrSetAsync(url, await LoadDataFromExternalAsync(url))
                .ConfigureAwait(false);
            if (externalBytes is null)
                return null;

            using var memoryStream = new MemoryStream(externalBytes);
            var bitmap = new Bitmap(memoryStream);
            return bitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Dispose() { }

    /// <summary>
    ///     the url maybe is local file url,so if file exists ,we got a Bitmap
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static Task<Bitmap?> LoadFromLocalAsync(string url)
    {
        return Task.FromResult(File.Exists(url) ? new Bitmap(url) : null);
    }

    /// <summary>
    ///     Receives image bytes from an internal source (for example, from the disk).
    ///     This data will be NOT cached globally (because it is assumed that it is already in internal source us and does not
    ///     require global caching)
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Bitmap</returns>
    private Task<Bitmap?> LoadFromInternalAsync(string url)
    {
        try
        {
            var uri = url.StartsWith('/')
                ? new Uri(url, UriKind.Relative)
                : new Uri(url, UriKind.RelativeOrAbsolute);

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                return Task.FromResult<Bitmap?>(null);

            if (uri is { IsAbsoluteUri: true, IsFile: true })
                return Task.FromResult(new Bitmap(uri.LocalPath))!;

            return Task.FromResult(new Bitmap(AssetLoader.Open(uri)))!;
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to resolve image from request with uri: {RequestUri}\nException: {Exception}",
                url,
                e
            );
            return Task.FromResult<Bitmap?>(null);
        }
    }

    /// <summary>
    ///     Receives image bytes from an external source (for example, from the Internet).
    ///     This data will be cached globally (if required by the current implementation)
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Image bytes</returns>
    private static async Task<byte[]?> LoadDataFromExternalAsync(string url)
    {
        try
        {
            return await url.GetBytesAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
