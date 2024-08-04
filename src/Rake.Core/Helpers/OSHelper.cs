using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Rake.Core.Helpers;

// ReSharper disable once InconsistentNaming
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class OSHelper
{
    public static bool IsWindows => GetOSPlatform() == OSPlatform.Windows;
    public static bool IsLinux => GetOSPlatform() == OSPlatform.Linux;
    public static bool IsOSX => GetOSPlatform() == OSPlatform.OSX;
    public static Platform Platform => GetPlatform();
    public static OSPlatform OSPlatform => GetOSPlatform();

    /// <summary>
    /// Gets the <see cref="Platform"/> depending on what platform you are on
    /// </summary>
    /// <returns>Returns the OS Version</returns>
    /// <exception cref="Exception"></exception>
    public static OSPlatform GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSPlatform.Linux;
        }

        throw new Exception("Your OS isn't supported");
    }

    /// <summary>
    /// Gets the <see cref="Platform"/> depending on what platform you are on
    /// </summary>
    /// <returns>Returns the OS Version</returns>
    /// <exception cref="Exception"></exception>
    public static Platform GetPlatform()
    {
        if (IsWindows)
        {
            return Platform.Windows;
        }

        if (IsLinux)
        {
            return Platform.Linux;
        }

        return IsOSX ? Platform.OSX : Platform.NotSupported;
    }
}

public enum Platform
{
    Windows,
    Linux,

    // ReSharper disable once InconsistentNaming
    OSX,
    NotSupported
}
