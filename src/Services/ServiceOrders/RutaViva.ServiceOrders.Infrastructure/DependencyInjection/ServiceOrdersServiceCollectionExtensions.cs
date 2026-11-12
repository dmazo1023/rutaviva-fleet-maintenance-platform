using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.DependencyInjection;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.BuildingBlocks.EventBus.Client.Publishing;
using RutaViva.BuildingBlocks.EventBus.Client.Subscribing;
using RutaViva.BuildingBlocks.Infrastructure.DependencyInjection;
using RutaViva.BuildingBlocks.Infrastructure.DomainEvents;
using RutaViva.BuildingBlocks.Infrastructure.Time;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Customers;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;
using RutaViva.ServiceOrders.Application.Orders;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Infrastructure.Persistence;
using RutaViva.ServiceOrders.Infrastructure.Persistence.Queries;
using RutaViva.ServiceOrders.Infrastructure.Persistence.Repositories;

namespace RutaViva.ServiceOrders.Infrastructure.DependencyInjection;

public static class ServiceOrdersServiceCollectionExtensions
{
    private const string ServiceName = "serviceorders";
    private const string ConnectionStringName = "RutaViva";
    private const string HubUrlKey = EventBusClientOptions.SectionName + ":HubUrl";
    private const string AutoApprovalMaxAmountKey = "ServiceOrders:AutoApprovalMaxAmount";
    private const decimal DefaultAutoApprovalMaxAmount = 1_000_000m;
    private const string AutomaticOrdersSinceKey = "ServiceOrders:AutomaticOrdersSince";
    private const string MissingConnectionStringMessage = "Connection string 'RutaViva' is not configured.";

    public static IServiceCollection AddServiceOrders(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddServiceOrdersPersistence(configuration);
        services.AddServiceOrdersRepositories();
        services.AddServiceOrdersApplication(configuration);
        services.AddServiceOrdersEventBus(configuration);
        return services;
    }

    private static void AddServiceOrdersPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(MissingConnectionStringMessage);
        services.AddRutaVivaPersistence<ServiceOrdersDbContext>(ServiceName);
        services.AddDbContext<ServiceOrdersDbContext>((serviceProvider, options) => ConfigureDbContext(serviceProvider, options, connectionString));
        services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, ServiceOrdersUnitOfWork>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
    }

    private static void ConfigureDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure());
        options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>());
    }

    private static void AddServiceOrdersRepositories(this IServiceCollection services)
    {
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IServiceOrderQueries, ServiceOrderQueries>();
        services.AddScoped<IServiceOrderReferenceStore, ServiceOrderReferenceStore>();
    }

    private static void AddServiceOrdersApplication(this IServiceCollection services, IConfiguration configuration)
    {
        Assembly applicationAssembly = typeof(CreateServiceOrderCommand).Assembly;
        services.AddSingleton(ReadApprovalPolicy(configuration));
        services.AddSingleton(ReadAutomaticOrderPolicy(configuration));
        services.AddScoped<ServiceOrderReferenceResolver>();
        services.AddCommandHandlers(applicationAssembly);
        services.AddQueryHandlers(applicationAssembly);
        services.Scan(scan => scan
            .FromAssemblies(applicationAssembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventMapper<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    private static QuoteApprovalPolicy ReadApprovalPolicy(IConfiguration configuration)
    {
        string? value = configuration[AutoApprovalMaxAmountKey];
        decimal amount = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : DefaultAutoApprovalMaxAmount;
        Result<QuoteApprovalPolicy> policy = QuoteApprovalPolicy.Create(amount);
        return policy.IsSuccess ? policy.Value : throw new InvalidOperationException(policy.Error.Message);
    }

    private static AutomaticOrderPolicy ReadAutomaticOrderPolicy(IConfiguration configuration)
    {
        string? value = configuration[AutomaticOrdersSinceKey];
        DateTimeOffset since = DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed)
            ? parsed
            : DateTimeOffset.MaxValue;
        return new AutomaticOrderPolicy(since);
    }

    private static void AddServiceOrdersEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        Action<EventBusClientOptions> configure = options => options.HubUrl = ReadHubUrl(configuration);
        services.AddOutboxRelay<ServiceOrdersDbContext>(configure);
        services.AddEventBusSubscriber(ServiceName, StartPosition.Checkpoint, configure)
            .Handle<VehicleRegisteredV1, VehicleRegisteredHandler>(Streams.FleetVehicle)
            .Handle<VehicleStatusChangedV1, VehicleStatusChangedHandler>(Streams.FleetVehicle)
            .Handle<VehicleResponsibleAssignedV1, VehicleResponsibleAssignedHandler>(Streams.FleetVehicle)
            .Handle<WorkshopRegisteredV1, WorkshopRegisteredHandler>(Streams.WorkshopsWorkshop)
            .Handle<ApproverRegisteredV1, ApproverRegisteredHandler>(Streams.CustomersCustomer)
            .Handle<ApproverStatusChangedV1, ApproverStatusChangedHandler>(Streams.CustomersCustomer)
            .Handle<MaintenanceRoutineRegisteredV1, MaintenanceRoutineRegisteredHandler>(Streams.MaintenanceRoutine)
            .Handle<MaintenanceRoutineOverdueV1, MaintenanceRoutineOverdueHandler>(Streams.MaintenanceRoutine);
    }

    private static Uri? ReadHubUrl(IConfiguration configuration)
    {
        string? value = configuration[HubUrlKey];
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? hubUrl) ? hubUrl : null;
    }
}
