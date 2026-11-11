using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.DependencyInjection;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.BuildingBlocks.EventBus.Client.Publishing;
using RutaViva.BuildingBlocks.EventBus.Client.Subscribing;
using RutaViva.BuildingBlocks.Infrastructure.DependencyInjection;
using RutaViva.BuildingBlocks.Infrastructure.DomainEvents;
using RutaViva.BuildingBlocks.Infrastructure.Time;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.IntegrationContracts.ServiceOrders;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.IntegrationEvents.Inbound;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Queries;
using RutaViva.Maintenance.Infrastructure.Persistence.Repositories;
using RutaViva.Maintenance.Infrastructure.Scheduling;

namespace RutaViva.Maintenance.Infrastructure.DependencyInjection;

public static class MaintenanceInfrastructureServiceCollectionExtensions
{
    private const string ServiceName = "maintenance";
    private const string ConnectionStringName = "RutaViva";
    private const string HubUrlKey = EventBusClientOptions.SectionName + ":HubUrl";
    private const string MissingConnectionStringMessage = "Connection string 'RutaViva' is not configured.";

    public static IServiceCollection AddMaintenance(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMaintenancePersistence(configuration);
        services.AddMaintenanceRepositories();
        services.AddMaintenanceQueries();
        services.AddMaintenanceApplication();
        services.AddMaintenanceEventBus(configuration);
        services.AddMaintenanceScheduler(configuration);
        return services;
    }

    private static void AddMaintenancePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(MissingConnectionStringMessage);
        services.AddRutaVivaPersistence<MaintenanceDbContext>(ServiceName);
        services.AddDbContext<MaintenanceDbContext>((serviceProvider, options) => ConfigureDbContext(serviceProvider, options, connectionString));
        services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, MaintenanceUnitOfWork>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
    }

    private static void ConfigureDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure());
        options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>());
    }

    private static void AddMaintenanceRepositories(this IServiceCollection services)
    {
        services.AddScoped<IMaintenancePlanRepository, MaintenancePlanRepository>();
        services.AddScoped<IRoutineTypeRepository, RoutineTypeRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IOdometerReadingLog, OdometerReadingLog>();
        services.AddSingleton<IUnitOfWorkScopeFactory, UnitOfWorkScopeFactory>();
    }

    private static void AddMaintenanceQueries(this IServiceCollection services)
    {
        services.AddScoped<IMaintenancePlanQueries, MaintenancePlanQueries>();
        services.AddScoped<IAlertQueries, AlertQueries>();
        services.AddScoped<IOdometerReadingQueries, OdometerReadingQueries>();
        services.AddScoped<IRoutineTypeQueries, RoutineTypeQueries>();
    }

    private static void AddMaintenanceApplication(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(IngestTelemetryReadingCommand).Assembly;
        services.AddCommandHandlers(applicationAssembly);
        services.AddQueryHandlers(applicationAssembly);
        services.AddScopedImplementationsOf(applicationAssembly, typeof(IDomainEventHandler<>));
        services.AddScopedImplementationsOf(applicationAssembly, typeof(IIntegrationEventMapper<>));
    }

    private static void AddScopedImplementationsOf(this IServiceCollection services, Assembly assembly, Type serviceType)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(serviceType), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    private static void AddMaintenanceEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        Action<EventBusClientOptions> configure = options => options.HubUrl = ReadHubUrl(configuration);
        services.AddOutboxRelay<MaintenanceDbContext>(configure);
        services.AddEventBusSubscriber(ServiceName, StartPosition.Checkpoint, configure)
            .Handle<VehicleRegisteredV1, VehicleRegisteredHandler>(Streams.FleetVehicle)
            .Handle<VehicleStatusChangedV1, VehicleStatusChangedHandler>(Streams.FleetVehicle)
            .Handle<ServiceOrderCompletedV1, ServiceOrderCompletedHandler>(Streams.ServiceOrdersServiceOrder);
    }

    private static Uri? ReadHubUrl(IConfiguration configuration)
    {
        string? value = configuration[HubUrlKey];
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? hubUrl) ? hubUrl : null;
    }

    private static void AddMaintenanceScheduler(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeEvaluationScheduleOptions.FromConfiguration(configuration));
        services.AddHostedService<DailyTimeEvaluationScheduler>();
    }
}
