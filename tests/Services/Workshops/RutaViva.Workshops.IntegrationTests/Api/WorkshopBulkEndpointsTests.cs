using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Workshops.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class WorkshopBulkEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string WorkshopsRoute = "/api/v1/workshops";
    private const string CityQueryFormat = "/api/v1/workshops?city={0}";
    private const string MedellinCity = "Medellín";
    private const string ValidOutcome = "Valid";
    private const string SkippedOutcome = "Skipped";
    private const int SampleRowCount = 3;
    private const int MedellinRowCount = 2;

    [Fact]
    public async Task PostBulk_Should_ReturnThreeValidThenThreeSkippedRows_When_SampleCsvIsLoadedTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        IReadOnlyList<WorkshopBulkRow> rows = WorkshopSampleCsv.ReadWorkshops();

        using HttpResponseMessage first = await WorkshopBulkClient.PostAsync(client, rows, false, cancellationToken);
        IReadOnlyList<string> firstOutcomes = await WorkshopBulkClient.ReadOutcomesAsync(first, cancellationToken);
        using HttpResponseMessage second = await WorkshopBulkClient.PostAsync(client, rows, false, cancellationToken);
        IReadOnlyList<string> secondOutcomes = await WorkshopBulkClient.ReadOutcomesAsync(second, cancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        firstOutcomes.Should().HaveCount(SampleRowCount).And.OnlyContain(outcome => outcome == ValidOutcome);
        secondOutcomes.Should().HaveCount(SampleRowCount).And.OnlyContain(outcome => outcome == SkippedOutcome);
    }

    [Fact]
    public async Task GetWorkshops_Should_ReturnOnlyMedellinWorkshops_When_SampleCsvIsLoadedAndCityIsFiltered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage loaded = await WorkshopBulkClient.PostAsync(client, WorkshopSampleCsv.ReadWorkshops(), false, cancellationToken);

        JsonElement all = await client.GetFromJsonAsync<JsonElement>(WorkshopsRoute, cancellationToken);
        JsonElement filtered = await client.GetFromJsonAsync<JsonElement>(string.Format(CityQueryFormat, Uri.EscapeDataString(MedellinCity)), cancellationToken);

        loaded.StatusCode.Should().Be(HttpStatusCode.OK);
        all.GetArrayLength().Should().Be(SampleRowCount);
        filtered.GetArrayLength().Should().Be(MedellinRowCount);
    }
}
