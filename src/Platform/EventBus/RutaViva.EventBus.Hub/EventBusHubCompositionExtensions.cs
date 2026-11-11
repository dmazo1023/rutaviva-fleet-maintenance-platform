using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.EventBus.Application.Failures;
using RutaViva.EventBus.Application.Publishing;
using RutaViva.EventBus.Application.Queries;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.EventBus.Hub;

internal static class EventBusHubCompositionExtensions
{
    private const string ConnectionStringName = "RutaViva";
    private const string MissingConnectionStringMessage = "Connection string 'RutaViva' is not configured.";

    public static string GetRutaVivaConnectionString(this IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName)
        ?? throw new InvalidOperationException(MissingConnectionStringMessage);

    public static IServiceCollection AddEventBusHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<PublishEventCommand, long>, PublishEventCommandHandler>();
        services.AddScoped<IQueryHandler<GetEventsQuery, IReadOnlyList<EventRecord>>, GetEventsQueryHandler>();
        services.AddScoped<IQueryHandler<GetLatestOffsetQuery, long>, GetLatestOffsetQueryHandler>();
        services.AddScoped<ICommandHandler<ReportFailureCommand, bool>, ReportFailureCommandHandler>();
        return services;
    }

    public static IServiceCollection AddEventBusSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddOptions<JsonHubProtocolOptions>()
            .Configure<IOptions<JsonOptions>>((hubOptions, apiOptions) =>
            {
                foreach (JsonConverter converter in apiOptions.Value.SerializerOptions.Converters)
                {
                    hubOptions.PayloadSerializerOptions.Converters.Add(converter);
                }
            });
        return services;
    }
}
