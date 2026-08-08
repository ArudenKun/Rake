using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;

namespace Rake.Core;

[DependsOn(typeof(AbpMapperlyModule), typeof(AbpGuidsModule))]
public class RakeCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context
            .Services.AddHttpClient()
            .ConfigureHttpClientDefaults(builder =>
                builder.ConfigureHttpClient(client =>
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue(new ProductHeaderValue(RakeConsts.Name))
                    )
                )
            );
    }
}
