using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class VehicleBulkCommitTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string BulkRoute = "/api/v1/fleet/vehicles/bulk";
    private const string ValidOutcome = "Valid";
    private const int ExpectedSampleVehicles = 10;

    [Fact]
    public async Task PostBulk_Should_ReturnAllRowsValidAndPublishTenEvents_When_SampleVehiclesAreCommitted()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await SampleReplicaSeeder.SeedAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        BulkRequest<VehicleBulkRow> request = new(SampleVehicleRows.Read());

        using HttpResponseMessage response = await client.PostAsJsonAsync(BulkRoute, request, cancellationToken);
        JsonElement body = await BulkResponseReader.ReadBodyAsync(response, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BulkResponseReader.Outcomes(body).Should().HaveCount(ExpectedSampleVehicles).And.OnlyContain(outcome => outcome == ValidOutcome);
        (await FleetOutboxReader.CountVehicleRegisteredAsync(database, cancellationToken)).Should().Be(ExpectedSampleVehicles);
    }
}
