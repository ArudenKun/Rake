using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Exceptions;
using Rake.Core.Extensions;
using Rake.Core.Helpers;

namespace Rake.Core;

public static partial class FFmpeg
{
    [GeneratedRegex(@"Duration:\s(\d+):(\d+):(\d+\.\d+)")]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"time=(\d+):(\d+):(\d+\.\d+)")]
    private static partial Regex TimeRegex();

    public static async ValueTask ExecuteAsync(
        string arguments,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default
    )
    {
        var stdErrBuffer = new StringBuilder();

        var stdErrPipe = PipeTarget.Merge(
            // Collect error output in case of failure
            PipeTarget.ToStringBuilder(stdErrBuffer),
            // Collect progress output if requested
            progress?.Pipe(CreateProgressRouter) ?? PipeTarget.Null
        );

        try
        {
            await Cli.Wrap(BinariesHelper.FFmpegPath)
                .WithArguments(arguments)
                .WithStandardErrorPipe(stdErrPipe)
                .ExecuteAsync(cancellationToken);
        }
        catch (CommandExecutionException ex)
        {
            throw new InvalidOperationException(
                $"""
                FFmpeg command-line tool failed with an error.

                Standard error:
                {stdErrBuffer}
                """,
                ex
            );
        }
    }

    private static PipeTarget CreateProgressRouter(IProgress<double> progress)
    {
        var totalDuration = default(TimeSpan?);

        return PipeTarget.ToDelegate(line =>
        {
            // Extract total stream duration
            if (totalDuration is null)
            {
                // Need to extract all components separately because TimeSpan cannot directly
                // parse a time string that is greater than 24 hours.
                var totalDurationMatch = DurationRegex().Match(line);
                if (totalDurationMatch.Success)
                {
                    var hours = int.Parse(
                        totalDurationMatch.Groups[1].Value,
                        CultureInfo.InvariantCulture
                    );
                    var minutes = int.Parse(
                        totalDurationMatch.Groups[2].Value,
                        CultureInfo.InvariantCulture
                    );
                    var seconds = double.Parse(
                        totalDurationMatch.Groups[3].Value,
                        CultureInfo.InvariantCulture
                    );

                    totalDuration =
                        TimeSpan.FromHours(hours)
                        + TimeSpan.FromMinutes(minutes)
                        + TimeSpan.FromSeconds(seconds);
                }
            }

            if (totalDuration is null || totalDuration == TimeSpan.Zero)
                return;

            // Extract processed stream duration
            var processedDurationMatch = TimeRegex().Match(line);
            if (!processedDurationMatch.Success)
                return;
            {
                var hours = int.Parse(
                    processedDurationMatch.Groups[1].Value,
                    CultureInfo.InvariantCulture
                );
                var minutes = int.Parse(
                    processedDurationMatch.Groups[2].Value,
                    CultureInfo.InvariantCulture
                );
                var seconds = double.Parse(
                    processedDurationMatch.Groups[3].Value,
                    CultureInfo.InvariantCulture
                );

                var processedDuration =
                    TimeSpan.FromHours(hours)
                    + TimeSpan.FromMinutes(minutes)
                    + TimeSpan.FromSeconds(seconds);

                progress.Report(
                    (
                        processedDuration.TotalMilliseconds / totalDuration.Value.TotalMilliseconds
                    ).Clamp(0, 1)
                );
            }
        });
    }
}
