using Microsoft.Extensions.DependencyInjection;
using Rake.ViewModels.Abstractions;
using ServiceScan.SourceGenerator;

namespace Rake.Extensions;

public static partial class ServiceCollectionExtensions
{
    [GenerateServiceRegistrations(
        AssignableTo = typeof(ViewModelBase),
        Lifetime = ServiceLifetime.Transient,
        AsSelf = true
    )]
    public static partial IServiceCollection AddViewModels(this IServiceCollection services);
}
