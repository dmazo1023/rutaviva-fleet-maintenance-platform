using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Infrastructure.DomainEvents;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.Infrastructure.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddRutaVivaPersistence<TContext>(this IServiceCollection services, string serviceName)
        where TContext : DbContext
    {
        services.AddSingleton(new IntegrationEventSettings { ServiceName = serviceName });
        services.AddSingleton<IntegrationEventEnvelopeFactory>();
        services.AddSingleton<OutboxMessageFactory>();
        services.AddScoped<IIntegrationEventPublisher>(CreatePublisher<TContext>);
        services.AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
        services.AddScoped<IEventCheckpointStore, EfEventCheckpointStore<TContext>>();
        services.AddDomainEventsToOutbox();
        return services;
    }

    private static OutboxIntegrationEventPublisher CreatePublisher<TContext>(IServiceProvider serviceProvider)
        where TContext : DbContext
        => new(serviceProvider.GetRequiredService<TContext>(), serviceProvider.GetRequiredService<OutboxMessageFactory>());
}
