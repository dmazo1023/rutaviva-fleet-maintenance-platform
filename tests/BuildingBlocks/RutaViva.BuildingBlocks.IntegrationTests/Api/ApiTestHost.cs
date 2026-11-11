using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Api.Correlation;
using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Serialization;

namespace RutaViva.BuildingBlocks.IntegrationTests.Api;

public sealed class ApiTestHost : IAsyncLifetime
{
    private const string HostNotStartedMessage = "Host not started.";

    private readonly CapturingLoggerProvider _logs = new();
    private WebApplication? _app;
    private HttpClient? _client;

    internal CapturingLoggerProvider Logs => _logs;

    public HttpClient Client => _client ?? throw new InvalidOperationException(HostNotStartedMessage);

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(_logs);
        builder.Services.AddApiJsonConventions();

        _app = builder.Build();
        _app.UseCorrelationId();
        _app.MapEndpointModules(typeof(ApiTestHost).Assembly);

        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
