using Microsoft.Extensions.DependencyInjection;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

public static class DomainEventsServiceCollectionExtensions
{
    public static IServiceCollection AddDomainEventsToOutbox(this IServiceCollection services)
    {
        services.AddSingleton<DomainEventCollector>();
        services.AddScoped<DomainEventHandlerDispatcher>();
        services.AddScoped<OutboxEventWriter>();
        services.AddScoped<DomainEventsToOutboxInterceptor>();
        return services;
    }
}
