using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RutaViva.BuildingBlocks.Application.DependencyInjection;
using RutaViva.EventBus.Application.Publishing;

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
        services.AddCommandHandlers(typeof(PublishEventCommand).Assembly);
        services.AddQueryHandlers(typeof(PublishEventCommand).Assembly);
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
