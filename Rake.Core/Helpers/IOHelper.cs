namespace Rake.Core.Helpers;

// ReSharper disable once InconsistentNaming
public static class IOHelper
{
    public const int DefaultBufferSize = 4096;

    public static bool DeleteIfExists(string path, bool recursive = true)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static async ValueTask CombineAsync(
        IEnumerable<string> files,
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var outputStream = File.Create(filePath);
        var total = 0;
        var filesArray = files.ToArray();
        foreach (var file in filesArray)
        {
            await using var inputStream = File.OpenRead(file);
            await inputStream.CopyToAsync(outputStream, cancellationToken);
            total++;
            progress?.Report(total / (double)filesArray.Length * 100.0 / 100.0);
        }
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

    public static FileStream OpenWrite(
        string filePath,
        bool overwrite = true,
        int bufferSize = DefaultBufferSize
    ) =>
        new(
            filePath,
            overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize
        );

    public static ValueTask<FileStream> OpenWriteAsync(
        string filePath,
        bool overwrite = true,
        int bufferSize = DefaultBufferSize
    ) =>
        ValueTask.FromResult(
            new FileStream(
                filePath,
                overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                true
            )
        );
}
