using Velopack.Locators;

namespace Rake.Core.Helpers;

public static class PathHelper
{
    private static VelopackLocator Locator => VelopackLocator.GetDefault(null);

    /// <summary>
    ///     Returns the friendly name of this application.
    /// </summary>
    public static string AppName => AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    ///     Returns the directory from which the application is run.
    /// </summary>
    public static string AppRootDirectory => Locator.RootAppDir ?? AppContext.BaseDirectory;

    public static string AppContentDirectory => Locator.AppContentDir ?? AppContext.BaseDirectory;

    /// <summary>
    ///     Returns the path of the ApplicationData.
    /// </summary>
    public static string DataDirectory =>
        // File.Exists(".portable") || Directory.Exists("data") ? AppRootDirectory.Combine("data") :
        Locator.IsPortable
            ? AppRootDirectory.Combine("data")
            : RoamingDirectory.Combine(AppName);

    // File.Exists(BaseDirectory.Combine(".portable"))
    // || Directory.Exists(BaseDirectory.Combine("portable"))
    //     ? BaseDirectory.Combine("portable")
    //     : RoamingDirectory.Combine(AppName);

    /// <summary>
    ///     Returns the path of the roaming directory.
    /// </summary>
    public static string RoamingDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    /// <summary>
    ///     Returns the directory of the user directory (ex: C:\Users\[the name of the user])
    /// </summary>
    public static string UserDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    ///     Returns the directory of the downloads directory
    /// </summary>
    public static string DownloadsDirectory => UserDirectory.Combine("Downloads");

    public static string LogsDirectory => DataDirectory.Combine("logs");

    public static string SettingsPath => DataDirectory.Combine("settings.json");

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

    /// <summary>
    ///     Returns the absolute path for the specified path string.
    ///     Also searches the environment's PATH variable.
    /// </summary>
    /// <param name="fileName">The relative path string.</param>
    /// <returns>The absolute path or null if the file was not found.</returns>
    public static string? GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
            return Path.GetFullPath(fileName);

        var values = Environment.GetEnvironmentVariable("PATH");

        return values
            ?.Split(Path.PathSeparator)
            .Select(p => Path.Combine(p, fileName))
            .FirstOrDefault(File.Exists);
    }

    public static string GetParent(int levels, string currentPath)
    {
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
        var paths = new Span<string>(new string[parts.Length + 1]) { [0] = path };
        for (var i = 0; i < parts.Length; i++)
        {
            paths[i + 1] = parts[i];
        }

        return Path.Combine(paths.ToArray());
    }

    // public static string Combine(this string path, params string[] parts)
    // {
    //     var paths = new List<string> { path };
    //     paths.AddRange(parts);
    //     return Path.Combine([.. paths]);
    // }
}
