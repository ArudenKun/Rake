namespace Rake.Core.YtDlp;

/// <summary>
/// Fluent configuration methods for Ytdlp.
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class YtDlp
{
    // ==================================================================================================================
    // NETWORK OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Use the specified HTTP/HTTPS/SOCKS proxy. To enable SOCKS proxy, specify a proper scheme, e.g. socks5://user:pass@127.0.0.1:1080/.
    /// </summary>
    /// <param name="proxy">Pass in an empty string for direct connection</param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithProxy(string? proxy)
    {
        if (proxy == null)
            return this;
        return AddOption("--proxy", proxy);
    }

    /// <summary>
    /// Time to wait before giving up, in seconds
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSocketTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            return this;
        double seconds = timeout.TotalSeconds;
        return AddOption("--socket-timeout", seconds.ToString("F0"));
    }

    /// <summary>
    /// Client-side IP address to bind to.
    /// </summary>
    /// <param name="ipAddress">The IP address to bind to.</param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSourceAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return this;
        return AddOption("--source-address", ipAddress);
    }

    /// <summary>
    /// Client to impersonate for requests. E.g. "chrome", "chrome-110", "chrome:windows-10".
    /// </summary>
    /// <param name="client">Pass string.Empty ("") to impersonate any client, or null to skip the option.</param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithImpersonate(string? client)
    {
        if (client == null)
            return this;
        return AddOption("--impersonate", client);
    }

    /// <summary>
    /// Impersonates any available client for requests (<c>--impersonate ""</c>).
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithImpersonateAny() => AddOption("--impersonate", string.Empty);

    /// <summary>
    /// Make all connections via IPv4
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithForceIpv4() => AddFlag("--force-ipv4");

    /// <summary>
    /// Make all connections via IPv6
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithForceIpv6() => AddFlag("--force-ipv6");

    /// <summary>
    /// Enable file:// URLs. This is disabled by default for security reasons.
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithEnableFileUrls() => AddFlag("--enable-file-urls");
}
