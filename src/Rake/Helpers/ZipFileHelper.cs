using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Rake.Extensions;

namespace Rake.Helpers;

public static class ZipFileHelper
{
    public static Task ExtractFileAsync(
        string zipPath,
        string filePath,
        string? entryName = null,
        bool overwrite = true,
        IProgress<double>? progress = null
    ) => Task.Run(() => ExtractFile(zipPath, filePath, entryName, overwrite, progress));

    public static void ExtractFile(
        string zipPath,
        string filePath,
        string? entryName = null,
        bool overwrite = true,
        IProgress<double>? progress = null
    )
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        using var archive = ZipFile.OpenRead(zipPath);
        entryName ??= Path.GetFileName(filePath);
        var entry =
            archive.GetEntry(entryName)
            ?? archive.Entries.FirstOrDefault(x => x.Name.Contains(entryName));

        ArgumentNullException.ThrowIfNull(entry);
        using var entryStream = entry.Open();
        using var filePathStream = IOHelper.OpenWrite(filePath, overwrite);
        entryStream.CopyTo(filePathStream, entry.Length, progress: progress);
    }

    public static Task ExtractDirectoryAsync(
        string zipPath,
        string outputDir,
        string? directoryToExtract = null,
        bool overwrite = true,
        IProgress<double>? progress = null
    ) =>
        Task.Run(
            () => ExtractDirectory(zipPath, outputDir, directoryToExtract, overwrite, progress)
        );

    public static void ExtractDirectory(
        string zipPath,
        string extractPath,
        string? directoryToExtract = null,
        bool overwrite = true,
        IProgress<double>? progress = null
    )
    {
        Directory.CreateDirectory(extractPath);
        using var archive = ZipFile.OpenRead(zipPath);
        directoryToExtract ??= Path.GetDirectoryName(extractPath)!;
        directoryToExtract = Path.TrimEndingDirectorySeparator(directoryToExtract);

        var entries = archive
            .Entries.Where(entry =>
                entry.FullName.Contains(directoryToExtract!, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(entry.Name)
            )
            .ToArray();

        var totalEntries = entries.Length;
        var entriesExtracted = 0;

        foreach (var entry in entries)
        {
            var relativePath =
                directoryToExtract == null
                    ? entry.FullName
                    : entry
                        .FullName[
                            (
                                entry.FullName.IndexOf(directoryToExtract, StringComparison.Ordinal)
                                + directoryToExtract.Length
                            )..
                        ]
                        .TrimStart('/');

            var destinationPath = Path.Combine(extractPath, relativePath);

            // Ensure the parent directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            entry.ExtractToFile(destinationPath, overwrite);

            entriesExtracted++;
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
            progress?.Report(1.0 * entriesExtracted / totalEntries);
        }
    }

    public static Task ExtractToDirectoryAsync(
        string zipPath,
        string destinationDir,
        IProgress<double>? progress = null
    ) => Task.Run(() => ExtractToDirectory(zipPath, destinationDir, progress));

    public static void ExtractToDirectory(
        string zipPath,
        string destinationDir,
        IProgress<double>? progress = null
    )
    {
        Directory.CreateDirectory(destinationDir);
        using var archive = ZipFile.OpenRead(zipPath);
        var totalEntries = archive.Entries.Count;
        var entriesExtracted = 0;
        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.Combine(destinationDir, entry.FullName);

            // Create directory if it doesn't exist
            if (entry.Name == "")
            {
                // Entry is a directory
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            // Create containing directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            // Extract file
            entry.ExtractToFile(destinationPath, overwrite: true);

            entriesExtracted++;
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
            progress?.Report(1.0 * entriesExtracted / totalEntries);
        }
    }
}
