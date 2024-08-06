using System.Threading.Channels;
using System.Threading.Tasks;

namespace Rake.Controls.Console;

public class ConsoleViewWriter
{
    private readonly ChannelWriter<string> _writer;

    internal ConsoleViewWriter(ChannelWriter<string> writer)
    {
        _writer = writer;
    }

    public async Task WriteAsync(string message)
    {
        await _writer.WriteAsync(message);
    }
}