using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using Rake.Core.Tools.FFmpeg;
using Rake.Core.Tools.HandBrake;
using Rake.Core.Tools.YtDlp;
using Volo.Abp.Guids;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using OctokitProductHeaderValue = Octokit.ProductHeaderValue;
using ProductHeaderValue = System.Net.Http.Headers.ProductHeaderValue;

namespace Rake.Core;

[DependsOn(typeof(AbpMapperlyModule), typeof(AbpGuidsModule))]
public class RakeCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IGitHubClient>(_ => new GitHubClient(
            new OctokitProductHeaderValue(RakeConsts.Name)
        ));

        var headerValue = new ProductHeaderValue(RakeConsts.Name);
        context.Services.AddHttpClient<IFFmpegToolsService, FFmpegToolsService>(client =>
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(headerValue))
        );
        context.Services.AddHttpClient<IYtDlpToolsService, YtDlpToolsService>(client =>
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(headerValue))
        );
        context.Services.AddHttpClient<IHandBrakeToolsService, HandBrakeToolsService>(client =>
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(headerValue))
        );
    }
}
