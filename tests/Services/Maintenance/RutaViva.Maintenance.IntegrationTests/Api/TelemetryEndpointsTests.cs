using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class TelemetryEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string OdometerReadingsRoute = "/api/v1/maintenance/odometer-readings";
    private const string UnknownPlate = "ZZZ999";
    private const string ReadAtText = "2026-11-12T08:00:00-05:00";
    private const long ValidKilometers = 15000L;
    private const long NegativeKilometers = -5L;
    private const string PlateProperty = "plate";
    private const string OdometerProperty = "odometerKm";
    private const string ReadAtProperty = "readAt";
    private const string OutcomeProperty = "outcome";
    private const string ReasonProperty = "reason";
    private const string IgnoredOutcome = "Ignored";
    private const string UnknownVehicleReason = "UnknownVehicle";

    [Fact]
    public async Task PostOdometerReading_Should_ReturnAcceptedWithIgnored_When_PlateIsUnknown()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            OdometerReadingsRoute,
            BuildReading(UnknownPlate, ValidKilometers),
            cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        body.GetProperty(OutcomeProperty).GetString().Should().Be(IgnoredOutcome);
        body.GetProperty(ReasonProperty).GetString().Should().Be(UnknownVehicleReason);
    }

    [Fact]
    public async Task PostOdometerReading_Should_ReturnBadRequest_When_KilometersAreNegative()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            OdometerReadingsRoute,
            BuildReading(UnknownPlate, NegativeKilometers),
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostOdometerReading_Should_ReturnBadRequest_When_PlateIsEmpty()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            OdometerReadingsRoute,
            BuildReading(string.Empty, ValidKilometers),
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Dictionary<string, object> BuildReading(string plate, long kilometers) =>
        new()
        {
            [PlateProperty] = plate,
            [OdometerProperty] = kilometers,
            [ReadAtProperty] = ReadAtText
        };
}
