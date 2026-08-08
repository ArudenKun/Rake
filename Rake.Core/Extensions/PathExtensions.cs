using System.Text.RegularExpressions;

namespace Rake.Core.Extensions;

public static class PathExtensions
{
    extension(Path)
    {
        public static string EnsureUniqueFilePath(string baseFilePath, int maxRetries = 100)
        {
            if (!File.Exists(baseFilePath))
                return baseFilePath;

            var baseDirPath = Path.GetDirectoryName(baseFilePath);
            var baseFileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFilePath);
            var baseFileExtension = Path.GetExtension(baseFilePath);

            for (var i = 1; i <= maxRetries; i++)
            {
                var fileName = $"{baseFileNameWithoutExtension} ({i}){baseFileExtension}";
                var filePath = !string.IsNullOrWhiteSpace(baseDirPath)
                    ? Path.Combine(baseDirPath, fileName)
                    : fileName;

                if (!File.Exists(filePath))
                    return filePath;
            }

            return baseFilePath;
        }
    }

    /// <summary>
    /// Replaces invalid file path characters in the input string with the specified replacement character.
    /// </summary>
    /// <param name="source">The path or file name string to sanitize.</param>
    /// <param name="replacementChar">The character to replace invalid characters with. Defaults to '_'.</param>
    /// <returns>A sanitized path string free of invalid characters.</returns>
    public static string Sanitize(this string source, char replacementChar = '_')
    {
        ArgumentNullException.ThrowIfNull(source);
        HashSet<char> blackList = [.. Path.GetInvalidFileNameChars(), '"']; // '"' not invalid in Linux, but causes problems
        var output = source.ToCharArray();
        for (int i = 0, ln = output.Length; i < ln; i++)
        {
            if (blackList.Contains(output[i]))
            {
                output[i] = replacementChar;
            }
        }
        return new string(output);
    }

    public static string CombinePath(this string path, params string[] parts)
    {
        var paths = new List<string> { path };
        paths.AddRange(parts);
        return Path.Combine([.. paths]);
    }
}
