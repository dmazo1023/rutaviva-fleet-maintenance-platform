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
using RutaViva.Fleet.Application.IntegrationEvents.Inbound;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Application.Vehicles.Queries;
using RutaViva.Fleet.Infrastructure.Persistence;
using RutaViva.Fleet.Infrastructure.Persistence.Repositories;
using RutaViva.Fleet.Infrastructure.Queries;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Customers;
using RutaViva.IntegrationContracts.Workshops;

namespace RutaViva.Fleet.Infrastructure.DependencyInjection;

public static class FleetInfrastructureServiceCollectionExtensions
{
    private const string ServiceName = "fleet";
    private const string ConnectionStringName = "RutaViva";
    private const string HubUrlKey = EventBusClientOptions.SectionName + ":HubUrl";
    private const string MissingConnectionStringMessage = "Connection string 'RutaViva' is not configured.";

    public static IServiceCollection AddFleet(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddFleetPersistence(configuration);
        services.AddFleetRepositories();
        services.AddFleetQueries();
        services.AddFleetApplication();
        services.AddFleetEventBus(configuration);
        return services;
    }

    private static void AddFleetPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(MissingConnectionStringMessage);
        services.AddRutaVivaPersistence<FleetDbContext>(ServiceName);
        services.AddDbContext<FleetDbContext>((serviceProvider, options) => ConfigureDbContext(serviceProvider, options, connectionString));
        services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FleetUnitOfWork>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
    }

    private static void ConfigureDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure());
        options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>());
    }

    private static void AddFleetRepositories(this IServiceCollection services)
    {
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleUniquenessChecker, VehicleUniquenessChecker>();
        services.AddScoped<IFleetReferenceReader, FleetReferenceStore>();
        services.AddScoped<IFleetReferenceWriter, FleetReferenceStore>();
    }

    private static void AddFleetQueries(this IServiceCollection services)
    {
        services.AddScoped<IVehicleQueries, VehicleQueries>();
    }

    private static void AddFleetApplication(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(RegisterVehicleCommand).Assembly;
        services.AddCommandHandlers(applicationAssembly);
        services.AddQueryHandlers(applicationAssembly);
        services.Scan(scan => scan
            .FromAssemblies(applicationAssembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventMapper<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    private static void AddFleetEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        Action<EventBusClientOptions> configure = options => options.HubUrl = ReadHubUrl(configuration);
        services.AddOutboxRelay<FleetDbContext>(configure);
        services.AddEventBusSubscriber(ServiceName, StartPosition.Checkpoint, configure)
            .Handle<CustomerRegisteredV1, CustomerRegisteredHandler>(Streams.CustomersCustomer)
            .Handle<CustomerStatusChangedV1, CustomerStatusChangedHandler>(Streams.CustomersCustomer)
            .Handle<ApproverRegisteredV1, ApproverRegisteredHandler>(Streams.CustomersCustomer)
            .Handle<ApproverStatusChangedV1, ApproverStatusChangedHandler>(Streams.CustomersCustomer)
            .Handle<WorkshopRegisteredV1, WorkshopRegisteredHandler>(Streams.WorkshopsWorkshop)
            .Handle<WorkshopStatusChangedV1, WorkshopStatusChangedHandler>(Streams.WorkshopsWorkshop);
    }

    private static Uri? ReadHubUrl(IConfiguration configuration)
    {
        string? value = configuration[HubUrlKey];
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? hubUrl) ? hubUrl : null;
    }
}
