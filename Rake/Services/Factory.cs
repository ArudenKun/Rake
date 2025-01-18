using System;
using System.Diagnostics.CodeAnalysis;
using AutoInterfaceAttributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rake.Services;

[AutoInterface]
public sealed class Factory<T>(Func<T> initFunc) : IFactory<T>
{
    public T Create() => initFunc();
}

[AutoInterface]
public sealed class KeyedFactory<T>(Func<object, T> initFunc) : IKeyedFactory<T>
{
    public T Create(object serviceKey) => initFunc(serviceKey);
}

public static class FactoryExtensions
{
    public static IServiceCollection AddKeyedFactory<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService
    >(this IServiceCollection services, object serviceKey)
        where TService : class
    {
        services.TryAddKeyedTransient<TService>(serviceKey);
        services.TryAddKeyedSingleton<Func<object, TService>>(
            serviceKey,
            (sp, _) => sp.GetRequiredKeyedService<TService>
        );
        services.TryAddSingleton<IKeyedFactory<TService>, KeyedFactory<TService>>();
        return services;
    }

    public static IServiceCollection AddKeyedFactory<TService>(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, TService> factory
    )
        where TService : class
    {
        services.TryAddKeyedTransient(serviceKey, (sp, _) => factory(sp));
        services.TryAddKeyedSingleton<Func<object, TService>>(
            serviceKey,
            (sp, _) => sp.GetRequiredKeyedService<TService>
        );
        services.TryAddSingleton<IKeyedFactory<TService>, KeyedFactory<TService>>();
        return services;
    }

    public static IServiceCollection AddFactory<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService
    >(this IServiceCollection services)
        where TService : class
    {
        services.TryAddTransient<TService>();
        services.TryAddSingleton<Func<TService>>(sp => sp.GetRequiredService<TService>);
        services.TryAddSingleton<IFactory<TService>, Factory<TService>>();
        return services;
    }

    public static IServiceCollection AddFactory<
        TService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TImplementation
    >(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.TryAddTransient<TService, TImplementation>();
        services.TryAddSingleton<Func<TService>>(sp => sp.GetRequiredService<TService>);
        services.TryAddSingleton<IFactory<TService>, Factory<TService>>();
        return services;
    }

    public static IServiceCollection AddFactory<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> factory
    )
        where TService : class
    {
        services.TryAddTransient(factory);
        services.TryAddSingleton<Func<TService>>(sp => sp.GetRequiredService<TService>);
        services.TryAddSingleton<IFactory<TService>, Factory<TService>>();
        return services;
    }
}
