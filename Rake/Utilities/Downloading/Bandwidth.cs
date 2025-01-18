﻿using System;
using System.Threading;

namespace Rake.Utilities.Downloading;

/// <summary>
/// Represents a class for calculating and managing bandwidth usage during a download operation.
/// </summary>
internal class Bandwidth
{
    private const double OneSecond = 1000; // millisecond
    private long _count;
    private int _lastSecondCheckpoint;
    private long _lastTransferredBytesCount;
    private int _speedRetrieveTime;

    /// <summary>
    /// Gets the current download speed in bytes per second.
    /// </summary>
    public double Speed { get; private set; }

    /// <summary>
    /// Gets the average download speed in bytes per second.
    /// </summary>
    public double AverageSpeed { get; private set; }

    /// <summary>
    /// Gets or sets the bandwidth limit in bytes per second.
    /// </summary>
    internal long BandwidthLimit { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bandwidth"/> class with default settings.
    /// </summary>
    public Bandwidth()
    {
        BandwidthLimit = long.MaxValue;
        Reset();
    }

    /// <summary>
    /// Calculates the current download speed based on the received bytes count.
    /// </summary>
    /// <param name="receivedBytesCount">The number of bytes received since the last calculation.</param>
    internal void CalculateSpeed(long receivedBytesCount)
    {
        int elapsedTime = Environment.TickCount - _lastSecondCheckpoint + 1;
        receivedBytesCount = Interlocked.Add(ref _lastTransferredBytesCount, receivedBytesCount);
        double momentSpeed = receivedBytesCount * OneSecond / elapsedTime; // B/s

        if (elapsedTime > OneSecond)
        {
            Speed = momentSpeed;
            AverageSpeed = (AverageSpeed * _count + Speed) / (_count + 1);
            _count++;
            SecondCheckpoint();
        }

        if (momentSpeed >= BandwidthLimit)
        {
            double expectedTime = receivedBytesCount * OneSecond / BandwidthLimit;
            Interlocked.Add(ref _speedRetrieveTime, (int)expectedTime - elapsedTime);
        }
    }

    /// <summary>
    /// Retrieves and resets the speed retrieve time.
    /// </summary>
    /// <returns>The speed retrieve time in milliseconds.</returns>
    internal int PopSpeedRetrieveTime()
    {
        return Interlocked.Exchange(ref _speedRetrieveTime, 0);
    }

    /// <summary>
    /// Resets the bandwidth calculation.
    /// </summary>
    internal void Reset()
    {
        SecondCheckpoint();
        _count = 0;
        Speed = 0;
        AverageSpeed = 0;
    }

    /// <summary>
    /// Sets the last second checkpoint to the current time and resets the transferred bytes count.
    /// </summary>
    private void SecondCheckpoint()
    {
        Interlocked.Exchange(ref _lastSecondCheckpoint, Environment.TickCount);
        Interlocked.Exchange(ref _lastTransferredBytesCount, 0);
    }
}