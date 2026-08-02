using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rake.Hosting.Internals;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Rake.Hosting;

/// <summary>
/// Provides extension methods for configuring Avalonia applications with the generic host.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    public static async Task AddApplicationAsync<TApplication, TStartupModule>(
        this IHostApplicationBuilder builder,
        Action<AppBuilder> appBuilderAction,
        Action<AbpApplicationCreationOptions>? optionsAction = null
    )
        where TApplication : Application
        where TStartupModule : IAbpModule
    {
        builder
            .Services.AddSingleton<TApplication>()
            .AddSingleton<Application>(sp => sp.GetRequiredService<TApplication>())
            .AddSingleton(sp =>
            {
                var appBuilder = AppBuilder.Configure(sp.GetRequiredService<TApplication>);
                appBuilderAction(appBuilder);
                return appBuilder;
            })
            .AddSingleton<AvaloniaThread>()
            .AddHostedService<AvaloniaHostedService>();

        await builder.Services.AddApplicationAsync<TStartupModule>(options =>
        {
            options.Services.ReplaceConfiguration(builder.Configuration);
            optionsAction?.Invoke(options);
            if (options.Environment.IsNullOrWhiteSpace())
            {
                options.Environment = builder.Environment.EnvironmentName;
            }
        });
    }

    public static async Task InitializeApplicationAsync(this IHost host)
    {
        Check.NotNull(host, nameof(host));

        host.Services.GetRequiredService<ObjectAccessor<IHost>>().Value = host;
        var application =
            host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
        await application.InitializeAsync(host.Services);
    }
}
