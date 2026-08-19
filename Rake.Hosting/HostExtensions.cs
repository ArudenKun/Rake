using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace Rake.Hosting;

public static class HostExtensions
{
    extension(IHost host)
    {
        public void InitializeApplication()
        {
            Check.NotNull(host, nameof(host));

            host.Services.GetRequiredService<ObjectAccessor<IHost>>().Value = host;
            var application =
                host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
            var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            applicationLifetime.ApplicationStopping.Register(application.Shutdown);
            applicationLifetime.ApplicationStopped.Register(application.Dispose);
            application.Initialize(host.Services);
        }

        public async Task InitializeApplicationAsync()
        {
            Check.NotNull(host, nameof(host));

            host.Services.GetRequiredService<ObjectAccessor<IHost>>().Value = host;
            var application =
                host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
            var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            applicationLifetime.ApplicationStopping.Register(() =>
                AsyncHelper.RunSync(application.ShutdownAsync)
            );
            applicationLifetime.ApplicationStopped.Register(application.Dispose);
            await application.InitializeAsync(host.Services);
        }
    }
}
