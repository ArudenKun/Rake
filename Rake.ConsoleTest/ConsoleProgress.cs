using Humanizer;
using Rake.Core;

namespace Rake.ConsoleTest;

internal class ConsoleProgress(TextWriter writer) : IProgress<DownloadProgressArgs>, IDisposable
{
    private readonly int _posX = Console.CursorLeft;
    private readonly int _posY = Console.CursorTop;
    private readonly Lock _writeLock = new();

    private int _lastLength;

    public ConsoleProgress()
        : this(Console.Out) { }

    private void EraseLast()
    {
        if (_lastLength > 0)
        {
            Console.SetCursorPosition(_posX, _posY);
            writer.Write(new string(' ', _lastLength));
            Console.SetCursorPosition(_posX, _posY);
        }
    }

    private void Write(string text)
    {
        EraseLast();
        writer.Write(text);
        _lastLength = text.Length;
    }

    public void Report(DownloadProgressArgs args)
    {
        lock (_writeLock)
        {
            Write(
                $"ETA: {args.Eta.Humanize()}|Speed: {args.Speed.Bytes().Humanize("#.##")}/s|Progress: {args.Percentage.Fraction:P1}|Downloaded: {args.Downloaded.Bytes().Humanize()}"
            );
        }
    }

    public void Dispose() => EraseLast();
}
