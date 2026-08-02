using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.Disposables;

namespace Rake.Core;

public sealed class IpcBroker : IDisposable
{
    private readonly string _pipeName;
    private readonly ILogger _logger;
    private CancellationTokenSource? _cts;

    public event Action? OnFocusRequested;

    public IpcBroker(string pipeName)
        : this(null, pipeName) { }

    public IpcBroker(ILogger? logger, string pipeName)
    {
        _logger = logger ?? NullLogger.Instance;
        _pipeName = pipeName;
    }

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts the background server listening for activation requests from other instances.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
            return;

        _cts = new CancellationTokenSource();
        Task.Run(() => ListenForClientsAsync(_cts.Token));
        IsRunning = true;
    }

    private async Task ListenForClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );
                await server.WaitForConnectionAsync(token);

                _logger.LogInformation("Another instance connected. Requesting focus.");

                // Trigger the UI thread event to wake up the main window
                OnFocusRequested?.Invoke();

                // Small delay to ensure clean disconnect before recycling the pipe
                await Task.Delay(500, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IPC named pipe server loop.");
                await Task.Delay(1000, token); // Prevent hot looping on failure
            }
        }
    }

    /// <summary>
    /// Notifies the running instance to take focus. Returns true if successful.
    /// </summary>
    public async Task<bool> SignalRunningInstanceAsync()
    {
        try
        {
            await using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            // Wait up to 2 seconds to connect to the existing instance
            await client.ConnectAsync(2000);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to the primary instance over IPC.");
            return false;
        }
    }

    public void Stop()
    {
        if (_cts is null)
            return;

        _cts?.Cancel();
        _cts?.Dispose();
        IsRunning = false;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
            return;

        await _cts.CancelAsync();
        await _cts.ToAsyncDisposable().DisposeAsync();
        IsRunning = false;
    }

    public void Dispose() => Stop();
}
