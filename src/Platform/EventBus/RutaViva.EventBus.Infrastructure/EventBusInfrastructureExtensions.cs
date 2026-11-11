using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Infrastructure.Time;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Infrastructure.Persistence;
using RutaViva.EventBus.Infrastructure.Storage;

namespace RutaViva.EventBus.Infrastructure;

public static class EventBusInfrastructureExtensions
{
    public static IServiceCollection AddEventBusInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContextFactory<EventBusDbContext>(options => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEventStore, SingleWriterEventStore>();
        return services;
    }
}
