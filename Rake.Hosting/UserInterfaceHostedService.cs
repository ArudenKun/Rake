﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rake.Hosting.Abstractions;

namespace Rake.Hosting;

/// <summary>
/// A long running service that will execute the User Interface
/// thread.
/// </summary>
/// <remarks>
/// <para>
/// Should be registered (only once) in the services collection with the
/// <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)">
/// AddHostedService
/// </see>
/// extension method.
/// </para>
/// <para>
/// Expects the <see cref="UserInterfaceThread" /> and <see cref="HostingContext" />
/// singleton instances to be setup in the dependency injector.
/// </para>
/// </remarks>
/// <param name="loggerFactory">
/// We inject a <see cref="ILoggerFactory" /> to be able to silently use a
/// <see cref="NullLogger" /> if we fail to obtain a <see cref="ILogger" />
/// from the Dependency Injector.
/// </param>
/// <param name="uiThread">
/// The <see cref="UserInterfaceThread" />
/// instance.
/// </param>
/// <param name="context">The <see cref="HostingContext" /> instance.</param>
public partial class UserInterfaceHostedService(
    ILoggerFactory? loggerFactory,
    IUserInterfaceThread uiThread,
    HostingContext context
) : IHostedService
{
    private readonly ILogger _logger =
        loggerFactory?.CreateLogger<UserInterfaceHostedService>()
        ?? NullLoggerFactory.Instance.CreateLogger<UserInterfaceHostedService>();

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        // Make the UI thread go
        uiThread.StartUserInterface();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || !context.IsRunning)
        {
            return Task.CompletedTask;
        }

        StoppingUserInterfaceThread();
        return uiThread.StopUserInterfaceAsync();
    }

    [LoggerMessage(
        SkipEnabledCheck = true,
        Level = LogLevel.Debug,
        Message = "Stopping User Interface Thread Due to Application Exiting"
    )]
    partial void StoppingUserInterfaceThread();
}
