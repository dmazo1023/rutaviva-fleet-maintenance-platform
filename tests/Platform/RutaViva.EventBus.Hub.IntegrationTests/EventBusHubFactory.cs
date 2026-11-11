using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RutaViva.EventBus.Hub.IntegrationTests;

internal sealed class EventBusHubFactory(string connectionString) : WebApplicationFactory<Program>
{
    private const string ConnectionSetting = "ConnectionStrings:RutaViva";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting(ConnectionSetting, connectionString);
}
