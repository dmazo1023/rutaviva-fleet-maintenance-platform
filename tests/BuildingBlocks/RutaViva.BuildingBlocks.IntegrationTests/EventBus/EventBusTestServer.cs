using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.EventBus.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus;

public sealed class EventBusTestServer : IAsyncLifetime
{
    private const string HostNotStartedMessage = "EventBus test server not started.";

    private readonly SqlServerTestDatabase _database = new();
    private EventBusHubApplication? _application;

    public string ConnectionString => _database.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _database.InitializeAsync();
        await using EventBusDbContext context = await _database.CreateDbContextAsync<EventBusDbContext>(TestContext.Current.CancellationToken);
        _application = new EventBusHubApplication(_database.ConnectionString);
        _ = _application.Server;
    }

    public void ConfigureClient(EventBusClientOptions options)
    {
        EventBusHubApplication application = _application ?? throw new InvalidOperationException(HostNotStartedMessage);
        options.HubUrl = application.Server.BaseAddress;
        options.HttpMessageHandlerFactory = application.Server.CreateHandler;
    }

    public async ValueTask DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }

        await _database.DisposeAsync();
    }

    private sealed class EventBusHubApplication(string connectionString) : WebApplicationFactory<Program>
    {
        private const string ConnectionSetting = "ConnectionStrings:RutaViva";

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseSetting(ConnectionSetting, connectionString);
    }
}
