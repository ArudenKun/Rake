using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Exceptions;
using Gress;
using Microsoft.Extensions.Options;
using PowerKit.Extensions;
using Volo.Abp.DependencyInjection;

namespace Rake.Core;

public partial class FFmpegService : ITransientDependency
{
    public FFmpegService(
        IOptions<RakeCoreOptions> options,
        HttpClient httpClient,
        IToolsService toolsService
    )
    {
        Options = options.Value;
        HttpClient = httpClient;
        ToolsService = toolsService;
    }

    protected RakeCoreOptions Options { get; }
    protected HttpClient HttpClient { get; }
    protected IToolsService ToolsService { get; }

    /// <summary>
    /// Executes an FFmpeg command with specified arguments.
    /// </summary>
    public ValueTask ExecuteAsync(string arguments, CancellationToken cancellationToken) =>
        ExecuteAsync(arguments, environmentVariables: null, progress: null, cancellationToken);

    /// <summary>
    /// Executes an FFmpeg command with progress tracking.
    /// </summary>
    public ValueTask ExecuteAsync(
        string arguments,
        IProgress<Percentage> progress,
        CancellationToken cancellationToken = default
    ) => ExecuteAsync(arguments, environmentVariables: null, progress, cancellationToken);

    /// <summary>
    /// Executes an FFmpeg command with custom environment variables.
    /// </summary>
    public ValueTask ExecuteAsync(
        string arguments,
        IReadOnlyDictionary<string, string?> environmentVariables,
        CancellationToken cancellationToken = default
    ) => ExecuteAsync(arguments, environmentVariables, progress: null, cancellationToken);

    /// <summary>
    /// Executes an FFmpeg command
    /// </summary>
    public async ValueTask ExecuteAsync(
        string arguments,
        IReadOnlyDictionary<string, string?>? environmentVariables = null,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        environmentVariables ??= new Dictionary<string, string?>();
        var stdErrBuffer = new StringBuilder();
        var stdErrPipe = PipeTarget.Merge(
            // Collect error output in case of failure
            PipeTarget.ToStringBuilder(stdErrBuffer),
            // Collect progress output if requested
            progress?.Pipe(CreateProgressRouter) ?? PipeTarget.Null
        );

        try
        {
            await Cli.Wrap(ToolsService.GetPath(Tool.FFmpeg))
                .WithArguments(arguments)
                .WithEnvironmentVariables(environmentVariables)
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

    private static PipeTarget CreateProgressRouter(IProgress<Percentage> progress)
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
            var processedDurationMatch = ProcessDurationRegex().Match(line);
            if (processedDurationMatch.Success)
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
                    Percentage.FromFraction(
                        (
                            processedDuration.TotalMilliseconds
                            / totalDuration.Value.TotalMilliseconds
                        ).Clamp(0, 1)
                    )
                );
            }
        });
    }

    [GeneratedRegex(@"Duration:\s(\d+):(\d+):(\d+\.\d+)")]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"time=(\d+):(\d+):(\d+\.\d+)")]
    private static partial Regex ProcessDurationRegex();
}
