using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rake.Hosting.Internals;

/// <summary>
/// Manages the Avalonia thread and application lifecycle.
/// </summary>
internal sealed class AvaloniaThread
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Thread _uiThread;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly TaskCompletionSource<object> _applicationExited = new();
    private readonly TaskCompletionSource<object> _applicationStarted = new();

    private SynchronizationContext? _synchronizationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaThread"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="applicationLifetime">The application lifetime.</param>
    public AvaloniaThread(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime applicationLifetime
    )
    {
        _serviceProvider = serviceProvider;
        _applicationLifetime = applicationLifetime;
        _uiThread = new Thread(ThreadStart) { Name = "Avalonia Thread", IsBackground = true };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _uiThread.SetApartmentState(ApartmentState.STA);
        }
    }

    internal Thread UiThread => _uiThread;

    internal SynchronizationContext SynchronizationContext =>
        _synchronizationContext
        ?? throw new InvalidOperationException("Avalonia Thread was not started");

    /// <summary>
    /// Starts the Avalonia thread.
    /// The task completes when <see cref="IControlledApplicationLifetime.Startup"/> event fires.
    /// </summary>
    public Task StartAsync(CancellationToken token)
    {
        _uiThread.Start();
        return _applicationStarted.Task.WaitAsync(token);
    }

    /// <summary>
    /// Stops the Avalonia thread.
    /// The task completes when <see cref="IControlledApplicationLifetime.Exit"/> event fires.
    /// </summary>
    public Task StopAsync(CancellationToken token)
    {
#pragma warning disable VSTHRD110
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (
                Application.Current?.ApplicationLifetime
                is ClassicDesktopStyleApplicationLifetime desktop
            )
                desktop.TryShutdown();
        });
#pragma warning restore VSTHRD110
        return _applicationExited.Task.WaitAsync(token);
    }

    /// <summary>
    /// The entry point for the Avalonia thread.
    /// </summary>
    private void ThreadStart()
    {
        try
        {
            var synchronizationContext = new AvaloniaSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            _synchronizationContext = synchronizationContext;

            var appBuilder = _serviceProvider.GetRequiredService<AppBuilder>();
            appBuilder.StartWithClassicDesktopLifetime(
                [],
                desktop =>
                {
                    desktop.Startup += (_, _) => _applicationStarted.SetResult(null!);
                    // desktop.ShutdownRequested += (_, _) =>
                    // {
                    //     // ReSharper disable once AccessToDisposedClosure
                    //     AsyncHelper.RunSync(application.ShutdownAsync);
                    // };
                    // desktop.Exit += (_, _) =>
                    // {
                    //     _applicationExited.TrySetResult(null!);
                    //     // ReSharper disable once AccessToDisposedClosure
                    //     application.Dispose();
                    //     _applicationLifetime.StopApplication();
                    // };
                }
            );
            // Avalonia stopped.
            _applicationExited.TrySetResult(null!);
            _applicationLifetime.StopApplication();
        }
        catch (Exception e)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<AvaloniaThread>>();
            logger.LogError(e, "Avalonia thread encountered an error");
            throw;
        }
    }
}
