using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.DependencyInjection;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.BuildingBlocks.EventBus.Client.Publishing;
using RutaViva.BuildingBlocks.Infrastructure.DependencyInjection;
using RutaViva.BuildingBlocks.Infrastructure.DomainEvents;
using RutaViva.BuildingBlocks.Infrastructure.Time;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Infrastructure.Persistence;
using RutaViva.Customers.Infrastructure.Persistence.Queries;
using RutaViva.Customers.Infrastructure.Persistence.Repositories;

namespace RutaViva.Customers.Infrastructure.DependencyInjection;

public static class CustomersInfrastructureServiceCollectionExtensions
{
    private const string ServiceName = "customers";
    private const string ConnectionStringName = "RutaViva";
    private const string HubUrlKey = EventBusClientOptions.SectionName + ":HubUrl";
    private const string MissingConnectionStringMessage = "Connection string 'RutaViva' is not configured.";

    public static IServiceCollection AddCustomers(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCustomersPersistence(configuration);
        services.AddCustomersRepositories();
        services.AddCustomersQueries();
        services.AddCustomersApplication();
        services.AddCustomersEventBus(configuration);
        return services;
    }

    private static void AddCustomersPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(MissingConnectionStringMessage);
        services.AddRutaVivaPersistence<CustomersDbContext>(ServiceName);
        services.AddDbContext<CustomersDbContext>((serviceProvider, options) => ConfigureDbContext(serviceProvider, options, connectionString));
        services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, CustomersUnitOfWork>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
    }

    private static void ConfigureDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure());
        options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>());
    }

    private static void AddCustomersRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerUniquenessChecker, CustomerUniquenessChecker>();
    }

    private static void AddCustomersQueries(this IServiceCollection services)
    {
        services.AddScoped<ICustomerQueries, CustomerQueries>();
    }

    private static void AddCustomersApplication(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(RegisterCustomerCommand).Assembly;
        services.AddCommandHandlers(applicationAssembly);
        services.AddQueryHandlers(applicationAssembly);
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

    private static void AddCustomersEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        Action<EventBusClientOptions> configure = options => options.HubUrl = ReadHubUrl(configuration);
        services.AddOutboxRelay<CustomersDbContext>(configure);
    }

    private static Uri? ReadHubUrl(IConfiguration configuration)
    {
        string? value = configuration[HubUrlKey];
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? hubUrl) ? hubUrl : null;
    }
}
