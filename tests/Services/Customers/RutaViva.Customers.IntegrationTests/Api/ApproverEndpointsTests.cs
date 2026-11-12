using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Customers.Api.Contracts;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class ApproverEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string CustomersRoute = "/api/v1/customers";
    private const string ApproversRouteFormat = "/api/v1/customers/{0}/approvers";
    private const string CustomerRouteFormat = "/api/v1/customers/{0}";
    private const string CustomerName = "Acme Transport";
    private const string CustomerEmail = "contact@acme.test";
    private const string CustomerPhone = "+573001234567";
    private const string CustomerTaxId = "920000001-1";
    private const string ConflictCustomerTaxId = "920000002-2";
    private const string ApproverName = "Laura Gomez";
    private const string ApproverEmail = "laura@acme.test";
    private const string CodeProperty = "code";
    private const string ApproversProperty = "approvers";
    private const string NotFoundCode = "Customer.NotFound";
    private const string EmailAlreadyRegisteredCode = "Customer.ApproverEmailAlreadyRegistered";

    [Fact]
    public async Task PostApprover_Should_ReturnCreatedWithCustomerLocation_When_RequestIsValid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Guid customerId = await RegisterCustomerAsync(client, CustomerTaxId, cancellationToken);

        using HttpResponseMessage response = await PostApproverAsync(client, customerId, ApproverEmail, cancellationToken);
        using HttpResponseMessage detail = await client.GetAsync(string.Format(CustomerRouteFormat, customerId), cancellationToken);
        JsonElement body = await detail.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.Should().Be(string.Format(CustomerRouteFormat, customerId));
        body.GetProperty(ApproversProperty).GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task PostApprover_Should_ReturnNotFound_When_CustomerDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await PostApproverAsync(client, Guid.NewGuid(), ApproverEmail, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty(CodeProperty).GetString().Should().Be(NotFoundCode);
    }

    [Fact]
    public async Task PostApprover_Should_ReturnConflictWithCode_When_EmailIsRepeatedInCustomer()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Guid customerId = await RegisterCustomerAsync(client, ConflictCustomerTaxId, cancellationToken);
        using HttpResponseMessage first = await PostApproverAsync(client, customerId, ApproverEmail, cancellationToken);

        using HttpResponseMessage second = await PostApproverAsync(client, customerId, ApproverEmail, cancellationToken);
        JsonElement body = await second.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body.GetProperty(CodeProperty).GetString().Should().Be(EmailAlreadyRegisteredCode);
    }

    private static async Task<Guid> RegisterCustomerAsync(HttpClient client, string taxId, CancellationToken cancellationToken)
    {
        RegisterCustomerRequest request = new(CustomerName, taxId, CustomerEmail, CustomerPhone);
        using HttpResponseMessage response = await client.PostAsJsonAsync(CustomersRoute, request, cancellationToken);
        CreatedResponse? created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created!.Id;
    }

    private static async Task<HttpResponseMessage> PostApproverAsync(
        HttpClient client,
        Guid customerId,
        string email,
        CancellationToken cancellationToken)
    {
        AddApproverRequest request = new(ApproverName, email);
        return await client.PostAsJsonAsync(string.Format(ApproversRouteFormat, customerId), request, cancellationToken);
    }
}
