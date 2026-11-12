using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class VehicleBulkEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string BulkRoute = "/api/v1/fleet/vehicles/bulk";
    private const string DryRunRoute = BulkRoute + "?dryRun=true";
    private const string ValidOutcome = "Valid";
    private const string InvalidOutcome = "Invalid";
    private const string OwnerNotFoundCode = "Vehicle.OwnerNotFound";
    private const string UnknownTaxId = "999999999";
    private const string BatchPlate = "BLK100";
    private const string BrandText = "Renault";
    private const string ModelText = "Kangoo";
    private const string WorkshopTaxId = "900100001";
    private const int ModelYear = 2022;
    private const int ExpectedSampleVehicles = 10;
    private const int MaximumRows = 500;
    private const int RowsOverMaximum = MaximumRows + 1;
    private const int ExpectedNoEvents = 0;
    private const long OdometerKm = 1000;

    [Fact]
    public async Task PostBulk_Should_ReturnValidRowsWithoutPersisting_When_DryRunIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await SampleReplicaSeeder.SeedAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        BulkRequest<VehicleBulkRow> request = new(SampleVehicleRows.Read());

        using HttpResponseMessage response = await client.PostAsJsonAsync(DryRunRoute, request, cancellationToken);
        JsonElement body = await BulkResponseReader.ReadBodyAsync(response, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BulkResponseReader.Outcomes(body).Should().HaveCount(ExpectedSampleVehicles).And.OnlyContain(outcome => outcome == ValidOutcome);
        (await FleetOutboxReader.CountVehicleRegisteredAsync(database, cancellationToken)).Should().Be(ExpectedNoEvents);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnOwnerNotFound_When_OwnerTaxIdIsUnknown()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        VehicleBulkRow row = BuildRow(1, UnknownTaxId);

        using HttpResponseMessage response = await client.PostAsJsonAsync(DryRunRoute, new BulkRequest<VehicleBulkRow>([row]), cancellationToken);
        JsonElement body = await BulkResponseReader.ReadBodyAsync(response, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BulkResponseReader.Outcomes(body).Should().Equal(InvalidOutcome);
        BulkResponseReader.FirstErrorCodes(body).Should().Equal(OwnerNotFoundCode);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnBadRequest_When_BatchExceedsFiveHundredRows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        List<VehicleBulkRow> rows = Enumerable.Range(1, RowsOverMaximum).Select(number => BuildRow(number, UnknownTaxId)).ToList();

        using HttpResponseMessage response = await client.PostAsJsonAsync(BulkRoute, new BulkRequest<VehicleBulkRow>(rows), cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static VehicleBulkRow BuildRow(int rowNumber, string ownerTaxId) =>
        new(rowNumber, BatchPlate, null, BrandText, ModelText, ModelYear, ownerTaxId, WorkshopTaxId, null, OdometerKm);
}
