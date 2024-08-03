using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Flurl;

namespace Rake.Core.Helpers;

public static class EnvironmentHelper
{
    private const string PortableFolder = "portable";

    /// <summary>
    ///     Returns the version of executing assembly.
    /// </summary>
    public static Version AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version();

    /// <summary>
    ///     Returns the friendly name of this application.
    /// </summary>
    public static string AppFriendlyName => AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    ///     Returns the path of the ApplicationData.
    /// </summary>
    public static string AppDataPath => GetApplicationDataPath();

    /// <summary>
    ///     Returns the directory from which the application is run.
    /// </summary>
    public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;

    public static string DownloadsDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).JoinPath("Downloads");

    /// <summary>
    ///     Indicates whether the current application should save its data in the AppDirectory
    /// </summary>
#pragma warning disable IO0003
    public static bool IsPortable => Directory.Exists(GetPortablePath());
#pragma warning restore IO0003

    /// <summary>
    ///     Gets or sets the <see cref="T:System.Globalization.CultureInfo" /> object that represents the culture used by the
    ///     current thread and task-based asynchronous operations.
    /// </summary>
    public static IFormatProvider CurrentCulture => CultureInfo.CurrentCulture;

    /// <summary>
    ///     Gets the application data folder path for the user.
    /// </summary>
    /// <param name="parts">Additional path parts to append to the path.</param>
    /// <returns>The application data folder path.</returns>
    public static string GetApplicationDataPath(params string[] parts)
    {
        string path;
        if (IsPortable)
        {
            path = GetPortablePath();
            IOHelper.EnsureContainingDirectoryExists(path);
            return path.JoinPath(parts);
        }

        path = GetDefaultPath();
        IOHelper.EnsureContainingDirectoryExists(path);
        return path.JoinPath(parts);
    }

    public static string GetPortablePath()
    {
        return PathHelper.GetParent(1, AppDirectory).JoinPath(PortableFolder);
    }

    public static string GetDefaultPath()
    {
        return Environment
            .GetFolderPath(Environment.SpecialFolder.ApplicationData)
            .JoinPath(AppFriendlyName);
    }

    public static void OpenUrl(Url url)
    {
        if (OSHelper.IsWindows)
        {
            Process.Start(
                new ProcessStartInfo(url.ToString().Replace("&", "^&")) { UseShellExecute = true }
            );
            return;
        }

        Process.Start("xdg-open", url);
    }

    /// <summary>
    /// Generates a random User-Agent from the IE browser.
    /// </summary>
    /// <returns>Random User-Agent from IE browser.</returns>
    // ReSharper disable once InconsistentNaming
    public static string IEUserAgent()
    {
        var windowsVersion = RandomWindowsVersion();

        string version;
        string mozillaVersion;
        string trident;
        string otherParams;

        #region Random version generation

        if (windowsVersion.Contains("NT 5.1"))
        {
            version = "9.0";
            mozillaVersion = "5.0";
            trident = "5.0";
            otherParams = ".NET CLR 2.0.50727; .NET CLR 3.5.30729";
        }
        else if (windowsVersion.Contains("NT 6.0"))
        {
            version = "9.0";
            mozillaVersion = "5.0";
            trident = "5.0";
            otherParams = ".NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729";
        }
        else
        {
            var random = Random.Shared;

            switch (random.Next(3))
            {
                case 0:
                    version = "10.0";
                    trident = "6.0";
                    mozillaVersion = "5.0";
                    break;

                case 1:
                    version = "10.6";
                    trident = "6.0";
                    mozillaVersion = "5.0";
                    break;

                default:
                    version = "11.0";
                    trident = "7.0";
                    mozillaVersion = "5.0";
                    break;
            }

            otherParams =
                ".NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E";
        }

        #endregion

        return $"Mozilla/{mozillaVersion} (compatible; MSIE {version}; {windowsVersion}; Trident/{trident}; {otherParams})";
    }

    /// <summary>
    /// Generates a random User-Agent from the Opera browser.
    /// </summary>
    /// <returns>A random User-Agent from the Opera browser.</returns>
    public static string OperaUserAgent()
    {
        string version;
        string presto;

        #region Random version generation

        var random = Random.Shared;

        switch (random.Next(4))
        {
            case 0:
                version = "12.16";
                presto = "2.12.388";
                break;

            case 1:
                version = "12.14";
                presto = "2.12.388";
                break;

            case 2:
                version = "12.02";
                presto = "2.10.289";
                break;

            default:
                version = "12.00";
                presto = "2.10.181";
                break;
        }

        #endregion

        return $"Opera/9.80 ({RandomWindowsVersion()}); U) Presto/{presto} Version/{version}";
    }

    /// <summary>
    /// Generates a random User-Agent from the Chrome browser.
    /// </summary>
    /// <returns>Random User-Agent from Chrome browser.</returns>
    public static string ChromeUserAgent()
    {
        var random = Random.Shared;

        var major = random.Next(62, 70);
        var build = random.Next(2100, 3538);
        var branchBuild = random.Next(170);

        return $"Mozilla/5.0 ({RandomWindowsVersion()}) AppleWebKit/537.36 (KHTML, like Gecko) "
            + $"Chrome/{major}.0.{build}.{branchBuild} Safari/537.36";
    }

    private static readonly byte[] FirefoxVersions = [64, 63, 62, 60, 58, 52, 51, 46, 45];

    /// <summary>
    /// Generates a random User-Agent from the Firefox browser.
    /// </summary>
    /// <returns>Random User-Agent from the Firefox browser.</returns>
    public static string FirefoxUserAgent()
    {
        var version = FirefoxVersions[Random.Shared.Next(FirefoxVersions.Length - 1)];

        return $"Mozilla/5.0 ({RandomWindowsVersion()}; rv:{version}.0) Gecko/20100101 Firefox/{version}.0";
    }

    /// <summary>
    /// Generates a random User-Agent from the Opera mobile browser.
    /// </summary>
    /// <returns>Random User-Agent from Opera mobile browser.</returns>
    public static string OperaMiniUserAgent()
    {
        string os;
        string miniVersion;
        string version;
        string presto;

        #region Random version generation

        var random = Random.Shared;

        switch (random.Next(3))
        {
            case 0:
                os = "iOS";
                miniVersion = "7.0.73345";
                version = "11.62";
                presto = "2.10.229";
                break;

            case 1:
                os = "J2ME/MIDP";
                miniVersion = "7.1.23511";
                version = "12.00";
                presto = "2.10.181";
                break;

            default:
                os = "Android";
                miniVersion = "7.5.54678";
                version = "12.02";
                presto = "2.10.289";
                break;
        }

        #endregion

        return $"Opera/9.80 ({os}; Opera Mini/{miniVersion}/28.2555; U; ru) Presto/{presto} Version/{version}";
    }

    /// <summary>
    /// Returns a random Chrome / Firefox / Opera User-Agent based on their popularity.
    /// </summary>
    /// <returns>User-Agent header value string</returns>
    public static string RandomUserAgent()
    {
        var rand = Random.Shared.Next(99) + 1;

        // TODO: edge, yandex browser, safari

        // Chrome = 70%
        if (rand >= 1 && rand <= 70)
            return ChromeUserAgent();

        // Firefox = 15%
        if (rand > 70 && rand <= 85)
            return FirefoxUserAgent();

        // IE = 6%
        if (rand > 85 && rand <= 91)
            return IEUserAgent();

        // Opera 12 = 5%
        if (rand > 91 && rand <= 96)
            return OperaUserAgent();

        // Opera mini = 4%
        return OperaMiniUserAgent();
    }

    private static string RandomWindowsVersion()
    {
        var random = Random.Shared;

        var windowsVersion = "Windows NT ";
        var val = random.Next(99) + 1;

        // Windows 10 = 45% popularity
        if (val >= 1 && val <= 45)
            windowsVersion += "10.0";
        // Windows 7 = 35% popularity
        else if (val > 45 && val <= 80)
            windowsVersion += "6.1";
        // Windows 8.1 = 15% popularity
        else if (val > 80 && val <= 95)
            windowsVersion += "6.3";
        // Windows 8 = 5% popularity
        else
            windowsVersion += "6.2";

        // Append WOW64 for X64 system
        if (random.NextDouble() <= 0.65)
            windowsVersion += random.NextDouble() <= 0.5 ? "; WOW64" : "; Win64; x64";

        return windowsVersion;
    }
}
