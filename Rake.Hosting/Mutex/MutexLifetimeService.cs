using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rake.Hosting.Abstractions;

namespace Rake.Hosting.Mutex;

/// <summary>
/// This maintains the mutex lifetime
/// </summary>
public sealed class MutexLifetimeService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IMutexBuilder _mutexBuilder;
    private LockFileMutex _lockFileMutex = null!;

    public MutexLifetimeService(
        ILogger<MutexLifetimeService> logger,
        IServiceProvider serviceProvider,
        IHostEnvironment hostEnvironment,
        IHostApplicationLifetime hostApplicationLifetime,
        IMutexBuilder mutexBuilder
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostEnvironment = hostEnvironment;
        _hostApplicationLifetime = hostApplicationLifetime;
        _mutexBuilder = mutexBuilder;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _lockFileMutex = LockFileMutex.Create(
            _logger,
            _mutexBuilder.MutexId,
            _hostEnvironment.ApplicationName
        );

        _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        _ = _lockFileMutex.Lock();
        if (_lockFileMutex.IsLocked)
        {
            var firstInstanceTask = _mutexBuilder.WhenFirstInstance?.Invoke(
                _serviceProvider,
                _logger,
                cancellationToken
            );

            if (firstInstanceTask is not null)
                await firstInstanceTask.ConfigureAwait(false);

            return;
        }

        var notFirstInstanceTask = _mutexBuilder.WhenNotFirstInstance?.Invoke(
            _serviceProvider,
            _logger,
            cancellationToken
        );

        if (notFirstInstanceTask is not null)
            await notFirstInstanceTask.ConfigureAwait(false);

        _logger.LogDebug(
            "Application {applicationName} already running, stopping application",
            _hostEnvironment.ApplicationName
        );
        _hostApplicationLifetime.StopApplication();
    }

    private void OnStopping()
    {
        _logger.LogDebug("Application is stopping, Disposing Lock.");
        if (_lockFileMutex.IsLocked)
        {
            _lockFileMutex.Dispose();
        }
        else
        {
            _lockFileMutex.Dispose(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
