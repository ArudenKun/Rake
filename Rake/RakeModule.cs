using System;
using System.IO;
using AsyncNavigation.Abstractions;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rake.Configuration.Writable;
using Rake.Configuration.Writable.FormatProvider;
using Rake.Core;
using Rake.Hosting;
using Rake.Settings;
using Rake.ViewModels;
using Rake.Views;
using Serilog.Core;
using Serilog.Events;
using ServiceScan.SourceGenerator;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Rake;

[DependsOn(typeof(RakeCoreModule), typeof(RakeHostingModule), typeof(AbpAutofacModule))]
public partial class RakeModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddWritableOptions<Setting>(builder =>
        {
            builder.FormatProvider = new JsonAotFormatProvider(Setting.SerializerContext.Default);
            builder
                .UseCustomDirectory(RakeDirectoryConsts.Data)
                .AddFilePath(RakePathConsts.Name.Setting);
        });
        var logLevel = configuration
            .GetRequiredSection("Logging")
            .GetValue<LogEventLevel>("LogLevel");
        context.Services.AddSingleton(new LoggingLevelSwitch(logLevel));
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<RakeCoreOptions>(options =>
        {
            options.ToolsDirectory = RakeDirectoryConsts.Tools;
        });

        ConfigureNavigation(context.Services);
        ConfigureVirtualFileSystem(context);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        base.OnApplicationInitialization(context);

#if DEBUG
        Ioc.Default.ConfigureServices(context.GetRootServiceProvider());
#endif
    }

    private void ConfigureNavigation(IServiceCollection services)
    {
        services.AddNavigationSupport();
        RegisterViewsAndViewModels(services);
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<RakeCoreModule>(
                    Path.Combine(
                        hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}Rake.Core"
                    )
                );
            });
        }
    }

    [ScanForTypes(
        AssignableTo = typeof(Window<>),
        Handler = nameof(RegisterViewsAndViewModelsHandler)
    )]
    [ScanForTypes(
        AssignableTo = typeof(SukiWindow<>),
        Handler = nameof(RegisterViewsAndViewModelsHandler)
    )]
    [ScanForTypes(
        AssignableTo = typeof(UserControl<>),
        Handler = nameof(RegisterViewsAndViewModelsHandler)
    )]
    private partial void RegisterViewsAndViewModels(IServiceCollection services);

    private static void RegisterViewsAndViewModelsHandler<TView, TViewModel>(
        IServiceCollection services
    )
        where TView : class, IView
        where TViewModel : ViewModel
    {
        services.RegisterView<TView, TViewModel>(typeof(TView).GetFullNameWithAssemblyName());
    }
}
