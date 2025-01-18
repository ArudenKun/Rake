using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gress;
using Humanizer;
using Rake.Core.Downloading;
using Rake.Utilities.Downloading;

namespace Rake.Extensions;

public static class StreamExtensions
{
    private const int DefaultBufferSize = 4096;

    public static void CopyTo(
        this Stream source,
        Stream destination,
        int bufferSize = DefaultBufferSize,
        long totalBytes = 0L,
        IProgress<Percentage>? progress = null
    ) => CopyTo(source, destination, bufferSize, totalBytes, progress.Wrap());

    public static void CopyTo(
        this Stream source,
        Stream destination,
        int bufferSize = DefaultBufferSize,
        long totalBytes = 0L,
        IProgress<ICopyProgress>? progress = null
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
            throw new ArgumentException("Source stream must be readable.", nameof(source));
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException(
                "Destination stream must be writable.",
                nameof(destination)
            );

        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);
        totalBytes =
            totalBytes == 0
                ? source.CanSeek
                    ? source.Length
                    : 0
                : totalBytes;

        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);
        using var buffer = MemoryPool<byte>.Shared.Rent(bufferSize);
        long totalBytesRead = 0;
        var bandwidth = new Bandwidth();
        var totalTime = Stopwatch.StartNew();
        while (true)
        {
            var bytesRead = source.Read(buffer.Memory.Span);
            if (bytesRead <= 0)
                break;

            destination.Write(buffer.Memory.Span[..bytesRead]);
            totalBytesRead += bytesRead;
            bandwidth.CalculateSpeed(bytesRead);
            progress?.Report(
                new CopyProgress(
                    totalTime.Elapsed,
                    bandwidth.Speed.Bytes(),
                    bandwidth.AverageSpeed.Bytes(),
                    totalBytesRead.Bytes(),
                    totalBytes.Bytes()
                )
            );
        }
    }

    public static ValueTask CopyToAsync(
        this Stream source,
        Stream destination,
        int bufferSize = DefaultBufferSize,
        long totalBytes = 0L,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default
    ) =>
        CopyToAsync(
            source,
            destination,
            bufferSize,
            totalBytes,
            progress.Wrap(),
            cancellationToken
        );

    public static async ValueTask CopyToAsync(
        this Stream source,
        Stream destination,
        int bufferSize = DefaultBufferSize,
        long totalBytes = 0L,
        IProgress<ICopyProgress>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("Source stream must be readable.", nameof(source));
        }

        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "Destination stream must be writable.",
                nameof(destination)
            );
        }

        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);
        totalBytes =
            totalBytes == 0
                ? source.CanSeek
                    ? source.Length
                    : 0
                : totalBytes;
        using var buffer = MemoryPool<byte>.Shared.Rent(bufferSize);
        long totalBytesRead = 0;
        var bandwidth = new Bandwidth();
        var totalTime = Stopwatch.StartNew();
        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer.Memory, cancellationToken);
            if (bytesRead <= 0)
                break;
            await destination.WriteAsync(buffer.Memory[..bytesRead], cancellationToken);
            totalBytesRead += bytesRead;
            bandwidth.CalculateSpeed(bytesRead);
            progress?.Report(
                new CopyProgress(
                    totalTime.Elapsed,
                    bandwidth.Speed.Bytes(),
                    bandwidth.AverageSpeed.Bytes(),
                    totalBytesRead.Bytes(),
                    totalBytes.Bytes()
                )
            );
            if (cancellationToken.IsCancellationRequested)
                break;
        }
    }
}
