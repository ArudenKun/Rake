using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Rake.Hosting;

public static class ApplicationInitializationContextExtensions
{
    public static IHost GetHost(this ApplicationInitializationContext context)
    {
        var host = context.ServiceProvider.GetRequiredService<IObjectAccessor<IHost>>().Value;
        if (host is null)
        {
            throw new InvalidOperationException("Host not initialized");
        }

        return host;
    }

    public static IServiceProvider GetRootServiceProvider(
        this ApplicationInitializationContext context
    )
    {
        var host = context.GetHost();
        return host.Services;
    }

    public static AppBuilder GetAppBuilder(this ApplicationInitializationContext context)
    {
        var appBuilder = context.ServiceProvider.GetRequiredService<AppBuilder>();
        return appBuilder;
    }

    public static IHostEnvironment GetEnvironment(this ApplicationInitializationContext context)
    {
        return context.ServiceProvider.GetRequiredService<IHostEnvironment>();
    }
}
