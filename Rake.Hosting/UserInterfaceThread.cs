using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rake.Hosting.Abstractions;

namespace Rake.Hosting;

/// <summary>
/// Implementation for a Avalonia based UI thread
/// </summary>
/// <param name="serviceProvider">
/// The Dependency Injector's <see cref="IServiceProvider" />.
/// </param>
/// <param name="lifetime">
/// The host application lifetime. Should be provided by the DI injector and is
/// used when the hosting context indicates that that the UI and the host
/// application lifetimes are linked.
/// </param>
/// <param name="context">
/// The UI service hosting context. Should be provided by the DI injector and
/// partially populated with the configuration options for the UI thread.
/// </param>
/// <param name="loggerFactory">
/// Used to obtain a logger for this class. If not possible, a <see cref="NullLogger" /> will be used instead.
/// </param>
public sealed class UserInterfaceThread(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    HostingContext context,
    ILoggerFactory? loggerFactory
)
    : BaseUserInterfaceThread<HostingContext>(
        lifetime,
        context,
        loggerFactory?.CreateLogger<UserInterfaceThread>() ?? MakeNullLogger()
    )
{
    /// <inheritdoc />
    public override Task StopUserInterfaceAsync()
    {
        Debug.Assert(
            HostingContext.Application is not null,
            "Expecting the `Application` in the context to not be null."
        );

        TaskCompletionSource completion = new();
        HostingContext.Dispatcher?.Invoke(() =>
        {
            if (
                HostingContext.Application.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime lifetime
            )
                return;
            lifetime.Shutdown();
            completion.SetResult();
        });
        return completion.Task;
    }

    /// <inheritdoc />
    protected override void PreStart() { }

    /// <inheritdoc />
    protected override void Start()
    {
        try
        {
            var appBuilder = serviceProvider.GetRequiredService<AppBuilder>();
            appBuilder.StartWithClassicDesktopLifetime([]);
            HostingContext.Dispatcher = Dispatcher.UIThread;
            var context = new AvaloniaSynchronizationContext(
                HostingContext.Dispatcher,
                DispatcherPriority.Default
            );
            SynchronizationContext.SetSynchronizationContext(context);
            HostingContext.Application = serviceProvider.GetRequiredService<Application>();

            /*
             * TODO: here we can add code that initializes the UI before the
             * main window is created and activated For example: unhandled
             * exception handlers, maybe instancing, activation, etc...
             */

            // NOTE: First window creation is to be handled in Application.OnLaunched()
        }
        catch (Exception e)
        {
            var logger = loggerFactory?.CreateLogger<UserInterfaceThread>();
            logger?.LogError(e, "An error occured while starting the application.");
            throw;
        }
    }

    private static ILogger MakeNullLogger() =>
        NullLoggerFactory.Instance.CreateLogger<UserInterfaceThread>();
}
