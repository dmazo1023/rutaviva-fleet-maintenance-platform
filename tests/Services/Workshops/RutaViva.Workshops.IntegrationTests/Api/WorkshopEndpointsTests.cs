using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Testing.Fixtures;
using RutaViva.Workshops.Api.Contracts;

namespace RutaViva.Workshops.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class WorkshopEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string WorkshopsRoute = "/api/v1/workshops";
    private const string LocationFormat = "/api/v1/workshops/{0}";
    private const string CityQueryFormat = "/api/v1/workshops?city={0}";
    private const string Name = "Taller de prueba";
    private const string Email = "taller@example.test";
    private const string Phone = "6044440000";
    private const string Address = "Calle 1 # 2-3";
    private const string CreatedTaxId = "900300001";
    private const string CreatedCity = "Manizales";
    private const string PereiraTaxIdOne = "900300002";
    private const string PereiraTaxIdTwo = "900300003";
    private const string BogotaTaxId = "900300004";
    private const string PereiraCity = "Pereira";
    private const string BogotaCity = "Bogotá";
    private const string InvalidTaxId = "abc";
    private const string TaxIdProperty = "taxId";
    private const int PereiraWorkshopCount = 2;

    [Fact]
    public async Task PostWorkshop_Should_ReturnCreatedWithLocation_When_RequestIsValid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await PostWorkshopAsync(client, CreatedTaxId, CreatedCity, cancellationToken);
        Guid id = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        using HttpResponseMessage detail = await client.GetAsync(response.Headers.Location, cancellationToken);
        JsonElement body = await detail.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Be(string.Format(LocationFormat, id));
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty(TaxIdProperty).GetString().Should().Be(CreatedTaxId);
    }

    [Fact]
    public async Task PostWorkshop_Should_ReturnBadRequest_When_TaxIdIsInvalid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await PostWorkshopAsync(client, InvalidTaxId, CreatedCity, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWorkshops_Should_ReturnOnlyMatchingCity_When_CityFilterIsApplied()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage first = await PostWorkshopAsync(client, PereiraTaxIdOne, PereiraCity, cancellationToken);
        using HttpResponseMessage second = await PostWorkshopAsync(client, PereiraTaxIdTwo, PereiraCity, cancellationToken);
        using HttpResponseMessage other = await PostWorkshopAsync(client, BogotaTaxId, BogotaCity, cancellationToken);

        JsonElement filtered = await client.GetFromJsonAsync<JsonElement>(string.Format(CityQueryFormat, PereiraCity), cancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        other.StatusCode.Should().Be(HttpStatusCode.Created);
        filtered.GetArrayLength().Should().Be(PereiraWorkshopCount);
    }

    [Fact]
    public async Task GetWorkshop_Should_ReturnNotFound_When_IdDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsApiFactory factory = await WorkshopsApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(string.Format(LocationFormat, Guid.NewGuid()), cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static Task<HttpResponseMessage> PostWorkshopAsync(HttpClient client, string taxId, string city, CancellationToken cancellationToken)
    {
        RegisterWorkshopRequest request = new(Name, taxId, Email, Phone, Address, city);
        return client.PostAsJsonAsync(WorkshopsRoute, request, cancellationToken);
    }
}
