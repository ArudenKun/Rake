using System;
using AutoInterfaceAttributes;
using Gress;
using Humanizer;
using Humanizer.Bytes;
using Rake.Core.Downloading;

namespace Rake.Utilities.Downloading;

[AutoInterface(Namespace = "Rake.Core.Downloading")]
internal readonly struct CopyProgress : ICopyProgress
{
    public CopyProgress(
        TimeSpan transferTime,
        ByteSize speed,
        ByteSize averageSpeed,
        ByteSize byteSizeRead,
        ByteSize totalByteSize
    )
    {
        TransferTime = transferTime;
        Speed = speed;
        AverageSpeed = averageSpeed;
        ByteSizeRead = byteSizeRead;
        TotalByteSize = totalByteSize;
    }

    public static readonly CopyProgress Complete = new(
        TimeSpan.MaxValue,
        long.MaxValue.Bytes(),
        long.MaxValue.Bytes(),
        long.MaxValue.Bytes(),
        long.MaxValue.Bytes()
    );

    /// <summary>
    /// The total time elapsed so far.
    /// </summary>
    public TimeSpan TransferTime { get; }

    /// <summary>
    /// The instantaneous data transfer rate.
    /// </summary>
    public ByteSize Speed { get; }

    public ByteSize AverageSpeed { get; }

    /// <summary>
    /// The total number of byte size transferred so far.
    /// </summary>
    public ByteSize ByteSizeRead { get; }

    /// <summary>
    /// The total number of bytes expected to be copied.
    /// </summary>
    public ByteSize TotalByteSize { get; }

    /// <summary>
    /// The percentage of the progress.
    /// </summary>
    public Percentage Percentage =>
        TotalByteSize.Bytes <= 0
            ? Percentage.FromValue(0)
            : Percentage.FromFraction(ByteSizeRead.Bytes / TotalByteSize.Bytes);
}
