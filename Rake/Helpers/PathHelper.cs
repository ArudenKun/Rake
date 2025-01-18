using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Velopack.Locators;

namespace Rake.Helpers;

public static class PathHelper
{
    private static readonly VelopackLocator Locator = VelopackLocator.GetDefault(null);

    /// <summary>
    ///     Returns the friendly name of this application.
    /// </summary>
    public const string AppName = nameof(Rake);

    /// <summary>
    ///     Returns the directory from which the application is run.
    /// </summary>
    public static readonly string AppRootDirectory = Locator.RootAppDir ?? AppContext.BaseDirectory;

    public static readonly string AppContentDirectory =
        Locator.AppContentDir ?? AppContext.BaseDirectory;

    // File.Exists(BaseDirectory.Combine(".portable"))
    // || Directory.Exists(BaseDirectory.Combine("portable"))
    //     ? BaseDirectory.Combine("portable")
    //     : RoamingDirectory.Combine(AppName);

    /// <summary>
    ///     Returns the path of the roaming directory.
    /// </summary>
    public static readonly string RoamingDirectory = Environment.GetFolderPath(
        Environment.SpecialFolder.ApplicationData
    );

    /// <summary>
    ///     Returns the directory of the user directory (ex: C:\Users\[the name of the user])
    /// </summary>
    public static string UserDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    ///     Returns the path of the ApplicationData.
    /// </summary>
    public static readonly string DataDirectory =
        // File.Exists(".portable") || Directory.Exists("data") ? AppRootDirectory.Combine("data") :
        Locator.IsPortable
            ? AppRootDirectory.Combine("data")
            : RoamingDirectory.Combine(AppName);

    /// <summary>
    ///     Returns the directory of the downloads directory
    /// </summary>
    public static readonly string DownloadsDirectory = UserDirectory.Combine("Downloads");

    public static readonly string LogsDirectory = DataDirectory.Combine("logs");

    public static readonly string SettingsPath = DataDirectory.Combine("settings.json");

    public static readonly string DatabasePath = DataDirectory.Combine("data.db");

    public static readonly string DatabaseConnectionString = $"Data Source={DatabasePath}";

    public static string GetTempDirectory(bool useBaseDir = true) =>
        useBaseDir
            ? AppRootDirectory.Combine(Path.GetFileNameWithoutExtension(GetTempFilePath()))
            : DataDirectory.Combine(Path.GetFileNameWithoutExtension(GetTempFilePath()));

    public static string GetTempFilePath(bool useBaseDirectory = true, string extension = "tmp")
    {
        // Trim the extension and ensure it starts with a dot
        var extSpan = extension.AsSpan().Trim();
        var finalExtension = extSpan.IsEmpty
            ? ".tmp".AsSpan()
            : (extSpan[0] != '.' ? "." + extension : extension).AsSpan();

        // Generate the random file name
        var randomFileName = Path.GetRandomFileName().Split('.')[0];
        var fileName = randomFileName + new string(finalExtension);

        // Return the combined path based on the specified directory
        return useBaseDirectory
            ? Path.Combine(AppRootDirectory, fileName)
            : Path.Combine(DataDirectory, fileName);
    }

    public static string GetTempFileName() => Path.GetFileName(GetTempFilePath());

    public static string GetTempFileNameWithoutExtension() =>
        Path.GetFileNameWithoutExtension(GetTempFilePath());

    public static string GetParent(string currentPath, int levels = 1)
    {
        if (levels is 0)
        {
            levels = 1;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(levels);
        var path = "";
        for (var i = 0; i < levels; i++)
            path += $"..{Path.DirectorySeparatorChar}";

        return Path.GetFullPath(Path.Combine(currentPath, path));
    }

    public static string SanitizeFileName(this string source, char replacementChar = '_')
    {
        var blackList = new HashSet<char>(Path.GetInvalidFileNameChars()) { '"' }; // '"' not invalid in Linux, but causes problems
        var output = source.ToCharArray();
        for (int i = 0, ln = output.Length; i < ln; i++)
            if (blackList.Contains(output[i]))
                output[i] = replacementChar;

        return new string(output);
    }

    public static string Combine(this string path, params string[] parts)
    {
        var paths = new List<string> { path };
        paths.AddRange(parts);
        return Path.Combine([.. paths]);
    }
}
