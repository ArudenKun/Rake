using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rake.Hosting;

public static class ServiceCollectionExtensions
{
    public static IHostEnvironment GetHostingEnvironment(this IServiceCollection services)
    {
        var hostingEnvironment = services.GetSingletonInstanceOrNull<IHostEnvironment>();
        if (hostingEnvironment == null)
        {
            return new EmptyHostingEnvironment { EnvironmentName = Environments.Development };
        }
        return hostingEnvironment;
    }
}
