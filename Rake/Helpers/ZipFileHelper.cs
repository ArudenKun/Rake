using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Gress;
using Rake.Extensions;

namespace Rake.Helpers;

public static class ZipFileHelper
{
    public static void ExtractFile(
        string zipPath,
        string filePath,
        string? entryName = null,
        bool overwrite = true,
        IProgress<Percentage>? progress = null
    )
    {
        using var zipStream = File.OpenRead(zipPath);
        ExtractFile(zipStream, filePath, entryName, overwrite, progress);
    }

    public static void ExtractFile(
        Stream zipStream,
        string filePath,
        string? entryName = null,
        bool overwrite = true,
        IProgress<Percentage>? progress = null
    )
    {
        using var archive = new ZipArchive(zipStream);
        entryName ??= Path.GetFileNameWithoutExtension(filePath);
        var entry =
            archive.GetEntry(entryName)
            ?? archive.Entries.FirstOrDefault(x => x.Name.Contains(entryName));

        ArgumentNullException.ThrowIfNull(entry);
        using var entryStream = entry.Open();
        using var filePathStream = IOHelper.OpenWrite(filePath, overwrite);
        entryStream.CopyTo(filePathStream, totalBytes: entry.Length, progress: progress);
    }

    public static async Task ExtractFileAsync(
        string zipPath,
        string filePath,
        string? entryName = null,
        bool overwrite = true,
        IProgress<Percentage>? progress = null
    )
    {
        await using var zipStream = File.OpenRead(zipPath);
        await ExtractFileAsync(zipStream, filePath, entryName, overwrite, progress);
    }

    public static async Task ExtractFileAsync(
        Stream zipStream,
        string filePath,
        string? entryName = null,
        bool overwrite = true,
        IProgress<Percentage>? progress = null
    )
    {
        using var archive = new ZipArchive(zipStream);
        entryName ??= Path.GetFileNameWithoutExtension(filePath);
        var entry =
            archive.GetEntry(entryName)
            ?? archive.Entries.FirstOrDefault(x => x.Name.Contains(entryName));

        ArgumentNullException.ThrowIfNull(entry);
        await using var entryStream = entry.Open();
        await using var fileStream = await IOHelper.OpenWriteAsync(filePath, overwrite);
        await entryStream.CopyToAsync(fileStream, totalBytes: entry.Length, progress: progress);
    }

    public static void ExtractDirectory(
        Stream zipStream,
        string destinationDir,
        string? directoryEntryName = null,
        bool overwrite = true,
        IProgress<Percentage>? progress = null
    )
    {
        try
        {
            Directory.CreateDirectory(destinationDir);
        }
        catch (Exception)
        {
            IOHelper.DeleteIfExists(destinationDir);
        }
    }

    // public static void ExtractDirectory(
    //     string zipPath,
    //     string extractPath,
    //     string? directoryToExtract = null,
    //     bool overwrite = true,
    //     IProgress<Percentage>? progress = null
    // )
    // {
    //     Directory.CreateDirectory(extractPath);
    //     using var archive = ZipFile.OpenRead(zipPath);
    //     directoryToExtract ??= Path.GetDirectoryName(extractPath)!;
    //     directoryToExtract = Path.TrimEndingDirectorySeparator(directoryToExtract);
    //
    //     var entries = archive
    //         .Entries.Where(entry =>
    //             entry.FullName.Contains(directoryToExtract!, StringComparison.OrdinalIgnoreCase)
    //             && !string.IsNullOrWhiteSpace(entry.Name)
    //         )
    //         .ToArray();
    //
    //     var totalEntries = entries.Length;
    //     var entriesExtracted = 0;
    //
    //     foreach (var entry in entries)
    //     {
    //         var relativePath =
    //             directoryToExtract == null
    //                 ? entry.FullName
    //                 : entry
    //                     .FullName[
    //                         (
    //                             entry.FullName.IndexOf(directoryToExtract, StringComparison.Ordinal)
    //                             + directoryToExtract.Length
    //                         )..
    //                     ]
    //                     .TrimStart('/');
    //
    //         var destinationPath = Path.Combine(extractPath, relativePath);
    //         Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
    //         entry.ExtractToFile(destinationPath, overwrite);
    //         entriesExtracted++;
    //         progress?.Report((double)entriesExtracted / totalEntries);
    //     }
    // }
    //
    // public static void ExtractToDirectory(
    //     string zipPath,
    //     string destinationDir,
    //     IProgress<Percentage>? progress = null
    // )
    // {
    //     Directory.CreateDirectory(destinationDir);
    //     using var archive = ZipFile.OpenRead(zipPath);
    //     var totalEntries = archive.Entries.Count;
    //     var entriesExtracted = 0;
    //     foreach (var entry in archive.Entries)
    //     {
    //         var destinationPath = Path.Combine(destinationDir, entry.FullName);
    //
    //         // Create directory if it doesn't exist
    //         if (entry.Name == "")
    //         {
    //             // Entry is a directory
    //             Directory.CreateDirectory(destinationPath);
    //             entriesExtracted++;
    //             continue;
    //         }
    //
    //         // Create containing directory if it doesn't exist
    //         Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
    //
    //         // Extract file
    //         entry.ExtractToFile(destinationPath, overwrite: true);
    //
    //         entriesExtracted++;
    //         progress?.Report((double)entriesExtracted / totalEntries);
    //     }
    // }
}
