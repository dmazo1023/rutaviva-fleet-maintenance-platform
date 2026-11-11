using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class TestEventPublisher : IAsyncDisposable
{
    private const string Source = "/rutaviva/testing";
    private const string TypeFormat = "rutaviva.testing.{0}.v1";

    private readonly HubConnection _connection;

    private TestEventPublisher(HubConnection connection)
    {
        _connection = connection;
    }

    public static string TypeFor(string eventName) => string.Format(TypeFormat, eventName);

    public static async Task<TestEventPublisher> ConnectAsync(EventBusTestServer server, CancellationToken cancellationToken)
    {
        EventBusClientOptions options = new();
        server.ConfigureClient(options);
        EventBusConnectionFactory factory = new(Options.Create(options));
        HubConnection connection = factory.Create();
        await connection.StartAsync(cancellationToken);
        return new TestEventPublisher(connection);
    }

    public Task<long> PublishAsync<TEvent>(string stream, string eventName, TEvent payload, string subject, Guid messageId, CancellationToken cancellationToken)
    {
        IntegrationEventEnvelope envelope = new()
        {
            Id = messageId,
            Source = Source,
            Type = TypeFor(eventName),
            Subject = subject,
            Time = DateTimeOffset.UtcNow,
            Data = JsonSerializer.SerializeToElement(payload, IntegrationEventJson.Options),
        };
        return _connection.InvokeAsync<long>(EventBusHubMethods.Publish, stream, envelope, cancellationToken);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
