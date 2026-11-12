using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class CustomerBulkLoadTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string CustomersRoute = "/api/v1/customers";
    private const string CustomerRouteFormat = "/api/v1/customers/{0}";
    private const string IdProperty = "id";
    private const string ApproversProperty = "approvers";
    private const int SampleCustomerRows = 3;
    private const int SampleApproverRows = 5;

    [Fact]
    public async Task PostBulk_Should_ReturnValidThenSkipped_When_SamplesAreLoadedTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        IReadOnlyList<BulkRowResult> customersLoaded = await PostCustomersAsync(client, false, cancellationToken);
        IReadOnlyList<BulkRowResult> approversSimulated = await PostApproversAsync(client, true, cancellationToken);
        int approversAfterSimulation = await CountApproversAsync(client, cancellationToken);
        IReadOnlyList<BulkRowResult> approversLoaded = await PostApproversAsync(client, false, cancellationToken);
        int approversAfterLoad = await CountApproversAsync(client, cancellationToken);
        IReadOnlyList<BulkRowResult> customersReloaded = await PostCustomersAsync(client, false, cancellationToken);
        IReadOnlyList<BulkRowResult> approversReloaded = await PostApproversAsync(client, false, cancellationToken);

        AssertOutcomes(customersLoaded, SampleCustomerRows, BulkRowOutcome.Valid);
        AssertOutcomes(approversSimulated, SampleApproverRows, BulkRowOutcome.Valid);
        approversAfterSimulation.Should().Be(0);
        AssertOutcomes(approversLoaded, SampleApproverRows, BulkRowOutcome.Valid);
        approversAfterLoad.Should().Be(SampleApproverRows);
        AssertOutcomes(customersReloaded, SampleCustomerRows, BulkRowOutcome.Skipped);
        AssertOutcomes(approversReloaded, SampleApproverRows, BulkRowOutcome.Skipped);
    }

    private static void AssertOutcomes(IReadOnlyList<BulkRowResult> results, int expectedRows, BulkRowOutcome expectedOutcome)
    {
        results.Should().HaveCount(expectedRows);
        results.Should().OnlyContain(result => result.Outcome == expectedOutcome);
        results.Should().OnlyContain(result => result.Errors.Count == 0);
    }

    private static Task<IReadOnlyList<BulkRowResult>> PostCustomersAsync(HttpClient client, bool dryRun, CancellationToken cancellationToken) =>
        BulkTestClient.PostForResultsAsync(client, BulkTestClient.CustomersBulkRoute, BulkSampleCsv.Customers(), dryRun, cancellationToken);

    private static Task<IReadOnlyList<BulkRowResult>> PostApproversAsync(HttpClient client, bool dryRun, CancellationToken cancellationToken) =>
        BulkTestClient.PostForResultsAsync(client, BulkTestClient.ApproversBulkRoute, BulkSampleCsv.Approvers(), dryRun, cancellationToken);

    private static async Task<int> CountApproversAsync(HttpClient client, CancellationToken cancellationToken)
    {
        JsonElement customers = await client.GetFromJsonAsync<JsonElement>(CustomersRoute, cancellationToken);
        int total = 0;
        foreach (JsonElement customer in customers.EnumerateArray())
        {
            total += await CountApproversOfAsync(client, customer.GetProperty(IdProperty).GetGuid(), cancellationToken);
        }

        return total;
    }

    private static async Task<int> CountApproversOfAsync(HttpClient client, Guid customerId, CancellationToken cancellationToken)
    {
        JsonElement detail = await client.GetFromJsonAsync<JsonElement>(string.Format(CustomerRouteFormat, customerId), cancellationToken);
        return detail.GetProperty(ApproversProperty).GetArrayLength();
    }
}
