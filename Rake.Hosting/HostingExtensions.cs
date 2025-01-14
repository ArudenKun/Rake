using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rake.Hosting.Abstractions;
using Rake.Hosting.Mutex;

namespace Rake.Hosting;

/// <summary>
/// Contains helper extensions for <see cref="HostApplicationBuilder" /> to
/// configure the WinUI service hosting.
/// </summary>
public static class HostingExtensions
{
    private const string MutexBuilderKey = "MutexBuilder";

    /// <summary>
    /// The key used to access the <see cref="HostingContext" /> instance in
    /// <see cref="IHostApplicationBuilder.Properties" />.
    /// </summary>
    public const string HostingContextKey = "UserInterfaceHostingContext";

    /// <summary>
    /// Configures the host builder for a Windows UI (WinUI) application.
    /// </summary>
    /// <typeparam name="TApplication">
    /// The concrete type for the <see cref="Application" /> class.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// This method configures the host builder to support a Windows UI (WinUI)
    /// application. It sets up the necessary services, including the hosting
    /// context, user interface thread, and the hosted service for the user
    /// interface.
    /// </para>
    /// <para>
    /// It attempts to find a <see cref="HostingContext" /> instance from the
    /// host builder properties and if not available creates one and adds it as
    /// a singleton service and as an <see cref="BaseHostingContext" /> service
    /// for use by the <see cref="UserInterfaceHostedService" />.
    /// </para>
    /// <para>
    /// Upon successful completion, the dependency injector will be able to
    /// provide the single instance of the application as a <typeparamref name="TApplication" />
    /// and as an <see cref="Application" /> if it is not the same type.
    /// </para>
    /// </remarks>
    /// <param name="hostBuilder">
    /// The host builder to which the WinUI service needs to be added.
    /// </param>
    /// <param name="appBuilderConfiguration"></param>
    /// <returns>The host builder for chaining calls.</returns>
    /// <exception cref="ArgumentException">
    /// When the application's type does not extend <see cref="Application" />.
    /// </exception>
    public static HostApplicationBuilder ConfigureAvalonia<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication
    >(this HostApplicationBuilder hostBuilder, Action<AppBuilder> appBuilderConfiguration)
        where TApplication : Application
    {
        HostingContext context;
        if (
            ((IHostApplicationBuilder)hostBuilder).Properties.TryGetValue(
                HostingContextKey,
                out var contextAsObject
            )
        )
        {
            context = (HostingContext)contextAsObject;
        }
        else
        {
            context = new HostingContext { IsLifetimeLinked = true };
            ((IHostApplicationBuilder)hostBuilder).Properties[HostingContextKey] = context;
        }

        _ = hostBuilder.Services.AddSingleton(context);

        _ = hostBuilder
            .Services.AddSingleton<IUserInterfaceThread, UserInterfaceThread>()
            .AddHostedService<UserInterfaceHostedService>();

        _ = hostBuilder.Services.AddSingleton<TApplication>();

        _ = hostBuilder.Services.AddSingleton(sp =>
        {
            var appBuilder = AppBuilder.Configure(sp.GetRequiredService<Application>);
            appBuilderConfiguration(appBuilder);
            return appBuilder;
        });

        if (typeof(TApplication) != typeof(Application))
        {
            _ = hostBuilder.Services.AddSingleton<Application>(services =>
                services.GetRequiredService<TApplication>()
            );
        }

        return hostBuilder;
    }

    /// <summary>
    /// Prevent that an application runs multiple times
    /// </summary>
    /// <param name="hostBuilder">IHostBuilder</param>
    /// <param name="configureAction">Action to configure IMutexBuilder</param>
    /// <returns>HostApplicationBuilder</returns>
    public static HostApplicationBuilder ConfigureSingleInstance(
        this HostApplicationBuilder hostBuilder,
        Action<IMutexBuilder>? configureAction = null
    )
    {
        if (
            !TryRetrieveMutexBuilder(
                ((IHostApplicationBuilder)hostBuilder).Properties,
                out var mutexBuilder
            )
        )
        {
            hostBuilder
                .Services.AddSingleton(mutexBuilder)
                .AddHostedService<MutexLifetimeService>();
        }

        configureAction?.Invoke(mutexBuilder);
        return hostBuilder;
    }

    /// <summary>
    /// Prevent that an application runs multiple times
    /// </summary>
    /// <param name="hostBuilder">IHostBuilder</param>
    /// <param name="mutexId">string</param>
    /// <returns>HostApplicationBuilder</returns>
    public static HostApplicationBuilder ConfigureSingleInstance(
        this HostApplicationBuilder hostBuilder,
        string mutexId
    ) => hostBuilder.ConfigureSingleInstance(builder => builder.MutexId = mutexId);

    /// <summary>
    /// Helper method to retrieve the mutex builder
    /// </summary>
    /// <param name="properties">IDictionary</param>
    /// <param name="mutexBuilder">IMutexBuilder out value</param>
    /// <returns>bool if there was a matcher</returns>
    private static bool TryRetrieveMutexBuilder(
        this IDictionary<object, object> properties,
        out IMutexBuilder mutexBuilder
    )
    {
        if (properties.TryGetValue(MutexBuilderKey, out var mutexBuilderObject))
        {
            mutexBuilder = (IMutexBuilder)mutexBuilderObject;
            return true;
        }

        mutexBuilder = new MutexBuilder();
        properties[MutexBuilderKey] = mutexBuilder;
        return false;
    }
}
