using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.Threading;
using Rake.Hosting.Internals;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Rake.Hosting;

/// <summary>
/// Provides extension methods for configuring Avalonia applications with the generic host.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddAvalonia<TApplication>(Action<AppBuilder> appBuilderAction)
            where TApplication : Application =>
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

        public void AddAvaloniaThreadSwitching() =>
            builder
                .Services.AddSingleton<JoinableTaskContext>(provider =>
                {
                    var avaloniaThread = provider.GetRequiredService<AvaloniaThread>();
                    return new JoinableTaskContext(
                        avaloniaThread.UiThread,
                        avaloniaThread.SynchronizationContext
                    );
                })
                .AddSingleton<JoinableTaskFactory>();

        public void AddApplication<TStartupModule>(
            Action<AbpApplicationCreationOptions>? optionsAction = null
        )
            where TStartupModule : IAbpModule =>
            builder.Services.AddApplication<TStartupModule>(options =>
            {
                options.Services.ReplaceConfiguration(builder.Configuration);
                optionsAction?.Invoke(options);
                if (options.Environment.IsNullOrWhiteSpace())
                {
                    options.Environment = builder.Environment.EnvironmentName;
                }
            });

        public async Task AddApplicationAsync<TStartupModule>(
            Action<AbpApplicationCreationOptions>? optionsAction = null
        )
            where TStartupModule : IAbpModule =>
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
}
