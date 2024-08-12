using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rake.Helpers;

// ReSharper disable once InconsistentNaming
public static class IOHelper
{
    public static void EnsureContainingDirectoryExists(string fileNameOrPath)
    {
        var fullPath = Path.GetFullPath(fileNameOrPath); // No matter if relative or absolute path is given to this.
        var dir = Path.GetDirectoryName(fullPath);
        EnsureDirectoryExists(dir);
    }

    /// <summary>
    ///     Makes sure that directory <paramref name="dir" /> is created if it does not exist.
    /// </summary>
    /// <remarks>Method does not throw exceptions unless provided directory path is invalid.</remarks>
    public static void EnsureDirectoryExists(string? dir)
    {
        // If root is given, then do not worry.
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static void DeleteDirectory(string dirPath)
    {
        foreach (var folder in Directory.GetDirectories(dirPath))
            DeleteDirectory(folder);

        foreach (var file in Directory.GetFiles(dirPath))
        {
            var pPath = Path.Combine(dirPath, file);
            File.SetAttributes(pPath, FileAttributes.Normal);
            File.Delete(file);
        }

        Directory.Delete(dirPath);
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

    public static FileStream OpenRead(string path, int bufferSize = 4096) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);

    public static Task<FileStream> OpenReadAsync(string path, int bufferSize = 4096) =>
        Task.FromResult(OpenRead(path, bufferSize));

    public static FileStream OpenWrite(
        string filePath,
        bool overwrite = true,
        int bufferSize = 4096
    )
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
        return new FileStream(
            filePath,
            overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            true
        );
    }

    public static Task<FileStream> OpenWriteAsync(
        string filePath,
        bool overwrite = true,
        int bufferSize = 4096
    )
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
        return Task.FromResult(OpenWrite(filePath, overwrite, bufferSize));
    }
}
