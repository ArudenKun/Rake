using System.Text;
using System.Text.RegularExpressions;
using AsyncAwaitBestPractices;
using CliWrap;
using CliWrap.Builders;
using CliWrap.Exceptions;
using Rake.Core.Extensions;
using Rake.Core.Helpers;

namespace Rake.Core;

public sealed partial class YtDlp : BaseCliWrapper
{
    private readonly WeakEventManager _progressWeakEventManager = new();

    public event EventHandler ProgressChanged
    {
        add => _progressWeakEventManager.AddEventHandler(value);
        remove => _progressWeakEventManager.RemoveEventHandler(value);
    }

    public ValueTask ExecuteAsync(
        Action<ArgumentsBuilder> argumentsBuilder,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var argumentsBuilderInstance = new ArgumentsBuilder();
        argumentsBuilder(argumentsBuilderInstance);
        return ExecuteAsync(argumentsBuilderInstance.Build(), progress, cancellationToken);
    }

    public override string CliName => "yt-dlp";

    public override async ValueTask ExecuteAsync(
        string arguments,
        IProgress<double>? progress = null,
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
            await Cli.Wrap(BinariesHelper.YtDlpPath)
                .WithArguments(arguments)
                .WithStandardErrorPipe(stdErrPipe)
                .ExecuteAsync(cancellationToken);
        }
        catch (CommandExecutionException ex)
        {
            throw new InvalidOperationException(
                $"""
                yt-dlp command-line tool failed with an error.

                Standard error:
                {stdErrBuffer}
                """,
                ex
            );
        }
    }

    protected override PipeTarget CreateProgressRouter(IProgress<double> progress)
    {
        return PipeTarget.Null;
    }

    private void OnProgressChanged() =>
        _progressWeakEventManager.RaiseEvent(this, EventArgs.Empty, nameof(ProgressChanged));

    [GeneratedRegex(@"Downloading video (\d+) of (\d+)", RegexOptions.Compiled)]
    private static partial Regex PlaylistRegex();

    [GeneratedRegex(
        @"\[download\]\s+(?:(?<percent>[\d\.]+)%(?:\s+of\s+\~?\s*(?<total>[\d\.\w]+))?\s+at\s+(?:(?<speed>[\d\.\w]+\/s)|[\w\s]+)\s+ETA\s(?<eta>[\d\:]+))?",
        RegexOptions.Compiled
    )]
    private static partial Regex ProgressRegex();

    [GeneratedRegex(@"\[(\w+)\]\s+", RegexOptions.Compiled)]
    private static partial Regex PostRegex();
}
