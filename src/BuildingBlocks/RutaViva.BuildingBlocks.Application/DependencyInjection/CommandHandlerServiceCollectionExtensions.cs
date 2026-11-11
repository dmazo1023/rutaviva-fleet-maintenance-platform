using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;

namespace RutaViva.BuildingBlocks.Application.DependencyInjection;

public static class CommandHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        RegisterHandlers(services, assembly);
        ValidatorRegistration.AddOnce(services, assembly);
        DecorateHandlers(services);
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    private static void DecorateHandlers(IServiceCollection services)
    {
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));
    }
}
