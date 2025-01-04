namespace Rake.Core.Helpers;

public static class HttpHelper
{
    private static readonly Lazy<HttpClient> LazyHttpClient = new(() =>
    {
        var handler = new HttpClientHandler { MaxConnectionsPerServer = int.MaxValue };
        var httpClient = new HttpClient(handler, true);
        httpClient.DefaultRequestHeaders.Add("User-Agent", ChromeUserAgent);
        return httpClient;
    });

    /// <summary>
    /// Lazy loaded <see cref="HttpClient"/>
    /// </summary>
    public static HttpClient HttpClient => LazyHttpClient.Value;

    /// <summary>
    /// Generates a random User-Agent from the Chrome browser.
    /// </summary>
    /// <returns>Random User-Agent from Chrome browser.</returns>
    public static string ChromeUserAgent
    {
        get
        {
            var random = Random.Shared;
            var major = random.Next(62, 70);
            var build = random.Next(2100, 3538);
            var branchBuild = random.Next(170);

            return $"Mozilla/5.0 ({RandomWindowsVersion()}) AppleWebKit/537.36 (KHTML, like Gecko) "
                + $"Chrome/{major}.0.{build}.{branchBuild} Safari/537.36";
        }
    }

    private static string RandomWindowsVersion()
    {
        var random = Random.Shared;

        var windowsVersion = "Windows NT ";
        var val = random.Next(99) + 1;

        windowsVersion += val switch
        {
            // Windows 10 = 45% popularity
            >= 1 and <= 45 => "10.0",
            // Windows 7 = 35% popularity
            > 45 and <= 80 => "6.1",
            // Windows 8.1 = 15% popularity
            > 80 and <= 95 => "6.3",
            _ => "6.2",
        };

        // Append WOW64 for X64 system
        if (random.NextDouble() <= 0.65)
            windowsVersion += random.NextDouble() <= 0.5 ? "; WOW64" : "; Win64; x64";

        return windowsVersion;
    }
}
