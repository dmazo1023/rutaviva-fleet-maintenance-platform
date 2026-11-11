using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using RutaViva.ServiceDefaults;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.ServiceDefaults;

[Trait("Category", "Integration")]
public sealed class ServiceDefaultsHealthTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string ConnectionKey = "ConnectionStrings:RutaViva";
    private const string AlivePath = "/alive";
    private const string HealthPath = "/health";
    private const string UnreachableConnection =
        "Server=rutaviva-missing.invalid;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=2;";

    [Fact]
    public async Task Alive_Should_ReturnOk_When_DatabaseIsUnreachable()
    {
        await using WebApplication app = await StartAsync(UnreachableConnection);
        using HttpClient client = app.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(AlivePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Should_ReturnServiceUnavailable_When_DatabaseIsUnreachable()
    {
        await using WebApplication app = await StartAsync(UnreachableConnection);
        using HttpClient client = app.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(HealthPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Health_Should_ReturnOk_When_DatabaseIsReachable()
    {
        await using WebApplication app = await StartAsync(database.ConnectionString);
        using HttpClient client = app.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(HealthPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<WebApplication> StartAsync(string connectionString)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [ConnectionKey] = connectionString });
        builder.AddServiceDefaults();
        WebApplication app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
