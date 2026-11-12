using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Workshops.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class WorkshopBulkDryRunEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string WorkshopsRoute = "/api/v1/workshops";
    private const string ValidOutcome = "Valid";
    private const string InvalidOutcome = "Invalid";
    private const string RowName = "Taller de prueba";
    private const string RowEmail = "taller@example.test";
    private const string RowPhone = "6044440000";
    private const string RowAddress = "Calle 1 # 2-3";
    private const string RowCity = "Cali";
    private const string RowTaxIdFormat = "9{0:D8}";
    private const string DuplicatedTaxId = "900200001";
    private const int SampleRowCount = 3;
    private const int MaxBulkRows = 500;
    private const int FirstRowNumber = 1;

    [Fact]
    public async Task PostBulk_Should_ReturnValidRowsWithoutPersisting_When_DryRunIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await WorkshopBulkClient.PostAsync(client, WorkshopSampleCsv.ReadWorkshops(), true, cancellationToken);
        IReadOnlyList<string> outcomes = await WorkshopBulkClient.ReadOutcomesAsync(response, cancellationToken);
        JsonElement stored = await client.GetFromJsonAsync<JsonElement>(WorkshopsRoute, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        outcomes.Should().HaveCount(SampleRowCount).And.OnlyContain(outcome => outcome == ValidOutcome);
        stored.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnInvalidForSecondRow_When_TaxIdRepeatsInsideTheBatch()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        List<WorkshopBulkRow> rows = [BuildRow(FirstRowNumber, DuplicatedTaxId), BuildRow(FirstRowNumber + 1, DuplicatedTaxId)];

        using HttpResponseMessage response = await WorkshopBulkClient.PostAsync(client, rows, true, cancellationToken);
        IReadOnlyList<string> outcomes = await WorkshopBulkClient.ReadOutcomesAsync(response, cancellationToken);

        outcomes.Should().Equal(ValidOutcome, InvalidOutcome);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnBadRequest_When_BatchExceedsMaximumRows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        List<WorkshopBulkRow> rows = Enumerable.Range(FirstRowNumber, MaxBulkRows + 1)
            .Select(number => BuildRow(number, string.Format(RowTaxIdFormat, number)))
            .ToList();

        using HttpResponseMessage response = await WorkshopBulkClient.PostAsync(client, rows, true, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static WorkshopBulkRow BuildRow(int rowNumber, string taxId) =>
        new(rowNumber, taxId, RowName, RowEmail, RowPhone, RowAddress, RowCity);
}
