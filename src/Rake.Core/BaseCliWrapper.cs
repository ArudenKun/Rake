using System.Text;
using CliWrap;
using CliWrap.Exceptions;
using Rake.Core.Extensions;
using Rake.Core.Helpers;

namespace Rake.Core;

public abstract class BaseCliWrapper
{
    public abstract string CliName { get; }
    protected abstract PipeTarget CreateProgressRouter(IProgress<double> progress);

    public virtual async ValueTask ExecuteAsync(
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
            await Cli.Wrap(BinariesHelper.FFmpegPath)
                .WithArguments(arguments)
                .WithStandardErrorPipe(stdErrPipe)
                .ExecuteAsync(cancellationToken);
        }
        catch (CommandExecutionException ex)
        {
            throw new InvalidOperationException(
                $"""
                {CliName} command-line tool failed with an error.

                Standard error:
                {stdErrBuffer}
                """,
                ex
            );
        }
    }
}
