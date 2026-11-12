using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Customers.Api.Contracts;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class CustomerEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string CustomersRoute = "/api/v1/customers";
    private const string CustomerRouteFormat = "/api/v1/customers/{0}";
    private const string CustomerName = "Acme Transport";
    private const string CustomerEmail = "contact@acme.test";
    private const string CustomerPhone = "+573001234567";
    private const string CreatedTaxId = "910000001-1";
    private const string DuplicatedTaxId = "910000002-2";
    private const string ListedTaxId = "910000003-3";
    private const string DetailTaxId = "910000005-5";
    private const string InvalidEmail = "not-an-email";
    private const string InvalidEmailTaxId = "910000004-4";
    private const string CodeProperty = "code";
    private const string TaxIdProperty = "taxId";
    private const string StatusProperty = "status";
    private const string ApproversProperty = "approvers";
    private const string ActiveStatus = "Active";
    private const string TaxIdAlreadyRegisteredCode = "Customer.TaxIdAlreadyRegistered";
    private const string NotFoundCode = "Customer.NotFound";

    [Fact]
    public async Task PostCustomer_Should_ReturnCreatedWithLocation_When_RequestIsValid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await PostCustomerAsync(client, CreatedTaxId, CustomerEmail, cancellationToken);
        CreatedResponse? body = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Be(string.Format(CustomerRouteFormat, body!.Id));
    }

    [Fact]
    public async Task PostCustomer_Should_ReturnConflictWithCode_When_TaxIdIsRepeated()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage first = await PostCustomerAsync(client, DuplicatedTaxId, CustomerEmail, cancellationToken);

        using HttpResponseMessage second = await PostCustomerAsync(client, DuplicatedTaxId, CustomerEmail, cancellationToken);
        JsonElement body = await second.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body.GetProperty(CodeProperty).GetString().Should().Be(TaxIdAlreadyRegisteredCode);
    }

    [Fact]
    public async Task PostCustomer_Should_ReturnBadRequest_When_EmailIsInvalid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await PostCustomerAsync(client, InvalidEmailTaxId, InvalidEmail, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomer_Should_ReturnNotFound_When_CustomerDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(string.Format(CustomerRouteFormat, Guid.NewGuid()), cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty(CodeProperty).GetString().Should().Be(NotFoundCode);
    }

    [Fact]
    public async Task GetCustomer_Should_ReturnDetail_When_LocationOfCreatedCustomerIsFollowed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage created = await PostCustomerAsync(client, DetailTaxId, CustomerEmail, cancellationToken);

        using HttpResponseMessage response = await client.GetAsync(created.Headers.Location, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty(TaxIdProperty).GetString().Should().Be(DetailTaxId);
        body.GetProperty(StatusProperty).GetString().Should().Be(ActiveStatus);
        body.GetProperty(ApproversProperty).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetCustomers_Should_IncludeRegisteredCustomer_When_NoFilterIsGiven()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage created = await PostCustomerAsync(client, ListedTaxId, CustomerEmail, cancellationToken);

        using HttpResponseMessage response = await client.GetAsync(CustomersRoute, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.EnumerateArray().Select(item => item.GetProperty(TaxIdProperty).GetString()).Should().Contain(ListedTaxId);
    }

    private static async Task<HttpResponseMessage> PostCustomerAsync(
        HttpClient client,
        string taxId,
        string email,
        CancellationToken cancellationToken)
    {
        RegisterCustomerRequest request = new(CustomerName, taxId, email, CustomerPhone);
        return await client.PostAsJsonAsync(CustomersRoute, request, cancellationToken);
    }
}
