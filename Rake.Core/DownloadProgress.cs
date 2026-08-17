using System.Diagnostics;
using Gress;

namespace Rake.Core;

/// <summary>
/// Provides progress data for an ongoing download operation.
/// </summary>
/// <param name="Percentage">The download progress</param>
/// <param name="Speed">The current download speed in bytes per second.</param>
/// <param name="AverageSpeed">The average download speed in bytes per second.</param>
/// <param name="Downloaded">The total number of bytes downloaded</param>
/// <param name="Eta">The estimated time of arrival (remaining time). Is <see langword="null"/> when completed or when the estimate cannot be calculated.</param>
public readonly record struct DownloadProgressArgs(
    Percentage Percentage,
    double Speed,
    double AverageSpeed,
    long Downloaded,
    TimeSpan Eta
);

public class DownloadProgress : IProgress<double>
{
    private readonly Stopwatch _stopwatch = new();
    private readonly Action<DownloadProgressArgs> _onProgressReported;
    private readonly Bandwidth _bandwidth = new();
    private readonly long _totalBytes;
    private long _lastBytesRead;

    /// <summary>
    /// Initializes a new instance of <see cref="DownloadProgress"/>.
    /// </summary>
    /// <param name="totalBytes">The total size of the download in bytes.</param>
    /// <param name="onProgressReported">
    /// Action callback providing: (double percentage, double speedMBps, long downloadedBytes)
    /// </param>
    public DownloadProgress(long totalBytes, Action<DownloadProgressArgs> onProgressReported)
    {
        _totalBytes = totalBytes;
        _onProgressReported = onProgressReported;
        _stopwatch.Start();
    }

    public void Report(double progress)
    {
        var currentBytes = (long)(progress * _totalBytes);
        var bytesDelta = currentBytes - _lastBytesRead;

        _bandwidth.CalculateSpeed(bytesDelta);
        _lastBytesRead = currentBytes;

        _onProgressReported(
            new DownloadProgressArgs(
                Percentage.FromFraction(progress),
                _bandwidth.Speed,
                _bandwidth.AverageSpeed,
                _lastBytesRead,
                CalculateEta(_bandwidth.Speed, _totalBytes - _lastBytesRead)
            )
        );
    }

    private static TimeSpan CalculateEta(double bytesPerSecond, long remainingBytes)
    {
        // If speed is zero, complete, or invalid, ETA cannot be reliably estimated
        if (bytesPerSecond <= 0 || remainingBytes <= 0)
            return TimeSpan.Zero;

        var remainingSeconds = remainingBytes / bytesPerSecond;

        // Cap at TimeSpan.MaxValue bounds safety check
        if (remainingSeconds > TimeSpan.MaxValue.TotalSeconds)
            return TimeSpan.Zero;

        return TimeSpan.FromSeconds(remainingSeconds);
    }
}
