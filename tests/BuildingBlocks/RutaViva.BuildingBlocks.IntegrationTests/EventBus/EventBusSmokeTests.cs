using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus;

[Trait("Category", "Integration")]
public sealed class EventBusSmokeTests(EventBusTestServer server) : IClassFixture<EventBusTestServer>
{
    [Fact]
    public async Task Create_Should_ConnectToHub_When_ClientIsConfiguredByTestServer()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        EventBusClientOptions options = new();
        server.ConfigureClient(options);
        EventBusConnectionFactory factory = new(Options.Create(options));
        await using HubConnection connection = factory.Create();

        await connection.StartAsync(cancellationToken);
        long latest = await connection.InvokeAsync<long>(EventBusHubMethods.GetLatestOffset, cancellationToken);

        latest.Should().Be(0);
    }
}
