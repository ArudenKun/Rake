using System.Reactive.Disposables;
using AutoInterfaceAttributes;
using CliWrap;
using Rake.Core.Helpers;

namespace Rake.Core.YtDlp;

[AutoInterface(Inheritance = [typeof(IDisposable)])]
public sealed class YtDlpClient : IYtDlpClient
{
    private readonly CompositeDisposable _disposables = new();
    private static string YtDlpPath => BinariesHelper.YtDlpPath;

    public async ValueTask<Video> GetVideoAsync(
        string url,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        await Cli.Wrap(YtDlpPath)
            .WithArguments(args => args.Add("-J").Add(url))
            .ExecuteAsync(cancellationToken);

        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
