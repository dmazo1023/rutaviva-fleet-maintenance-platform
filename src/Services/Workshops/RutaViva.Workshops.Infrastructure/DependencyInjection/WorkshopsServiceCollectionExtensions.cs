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
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Infrastructure.Persistence;
using RutaViva.Workshops.Infrastructure.Persistence.Queries;
using RutaViva.Workshops.Infrastructure.Persistence.Repositories;

namespace RutaViva.Workshops.Infrastructure.DependencyInjection;

public static class WorkshopsServiceCollectionExtensions
{
    private const string ServiceName = "workshops";
    private const string ConnectionStringName = "RutaViva";
    private const string HubUrlKey = EventBusClientOptions.SectionName + ":HubUrl";
    private const string MissingConnectionStringMessage = "Connection string 'RutaViva' is not configured.";

    public static IServiceCollection AddWorkshops(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddWorkshopsPersistence(configuration);
        services.AddWorkshopsRepositories();
        services.AddWorkshopsApplication();
        services.AddWorkshopsEventBus(configuration);
        return services;
    }

    private static void AddWorkshopsPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(MissingConnectionStringMessage);
        services.AddRutaVivaPersistence<WorkshopsDbContext>(ServiceName);
        services.AddDbContext<WorkshopsDbContext>((serviceProvider, options) => ConfigureDbContext(serviceProvider, options, connectionString));
        services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, WorkshopsUnitOfWork>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
    }

    private static void ConfigureDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure());
        options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>());
    }

    private static void AddWorkshopsRepositories(this IServiceCollection services)
    {
        services.AddScoped<IWorkshopRepository, WorkshopRepository>();
        services.AddScoped<IWorkshopUniquenessChecker, WorkshopUniquenessChecker>();
        services.AddScoped<IWorkshopQueries, WorkshopQueries>();
    }

    private static void AddWorkshopsApplication(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(RegisterWorkshopCommand).Assembly;
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

    private static void AddWorkshopsEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOutboxRelay<WorkshopsDbContext>(options => options.HubUrl = ReadHubUrl(configuration));
    }

    private static Uri? ReadHubUrl(IConfiguration configuration)
    {
        string? value = configuration[HubUrlKey];
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? hubUrl) ? hubUrl : null;
    }
}
