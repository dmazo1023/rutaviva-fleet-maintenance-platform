using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class CustomerBulkDryRunTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string CustomersRoute = "/api/v1/customers";
    private const string CustomerNotFoundCode = "Customer.NotFound";
    private const int SampleCustomerRows = 3;
    private const int SampleApproverRows = 5;

    [Fact]
    public async Task PostBulk_Should_PersistNoCustomers_When_DryRunIsTrue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        IReadOnlyList<BulkRowResult> results = await BulkTestClient.PostForResultsAsync(
            client,
            BulkTestClient.CustomersBulkRoute,
            BulkSampleCsv.Customers(),
            true,
            cancellationToken);
        JsonElement stored = await client.GetFromJsonAsync<JsonElement>(CustomersRoute, cancellationToken);

        results.Should().HaveCount(SampleCustomerRows);
        results.Should().OnlyContain(result => result.Outcome == BulkRowOutcome.Valid);
        stored.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnCustomerNotFound_When_ApproverCustomersAreNotLoaded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        IReadOnlyList<BulkRowResult> results = await BulkTestClient.PostForResultsAsync(
            client,
            BulkTestClient.ApproversBulkRoute,
            BulkSampleCsv.Approvers(),
            true,
            cancellationToken);

        results.Should().HaveCount(SampleApproverRows);
        results.Should().OnlyContain(result => result.Outcome == BulkRowOutcome.Invalid);
        results.Should().OnlyContain(result => result.Errors.Single().Code == CustomerNotFoundCode);
    }
}
