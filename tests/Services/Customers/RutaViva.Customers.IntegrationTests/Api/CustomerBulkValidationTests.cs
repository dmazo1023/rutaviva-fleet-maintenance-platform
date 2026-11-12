using System.Globalization;
using System.Net;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class CustomerBulkValidationTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const int MaxRows = 500;
    private const int FirstRowNumber = 1;
    private const long FirstGeneratedTaxId = 700000000;
    private const string GeneratedName = "Generated Customer";
    private const string GeneratedEmail = "generated@bulk.test";
    private const string GeneratedPhone = "6045550000";
    private const string RepeatedTaxId = "930000001";
    private const string InvalidTaxId = "RV-1";
    private const string ValidationFailedCode = "Validation.Failed";

    [Fact]
    public async Task PostBulk_Should_ReturnDuplicateInBatch_When_TaxIdRepeatsInBatch()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        IReadOnlyList<CustomerBulkRow> rows = [BuildRow(FirstRowNumber, RepeatedTaxId), BuildRow(FirstRowNumber + 1, RepeatedTaxId)];

        IReadOnlyList<BulkRowResult> results = await BulkTestClient.PostForResultsAsync(
            client,
            BulkTestClient.CustomersBulkRoute,
            rows,
            true,
            cancellationToken);

        results[0].Outcome.Should().Be(BulkRowOutcome.Valid);
        results[1].Outcome.Should().Be(BulkRowOutcome.Invalid);
        results[1].Errors.Should().ContainSingle().Which.Code.Should().Be(BulkErrorCodes.DuplicateInBatch);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnInvalidRow_When_TaxIdHasInvalidFormat()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        IReadOnlyList<CustomerBulkRow> rows = [BuildRow(FirstRowNumber, InvalidTaxId)];

        IReadOnlyList<BulkRowResult> results = await BulkTestClient.PostForResultsAsync(
            client,
            BulkTestClient.CustomersBulkRoute,
            rows,
            true,
            cancellationToken);

        results.Should().ContainSingle().Which.Outcome.Should().Be(BulkRowOutcome.Invalid);
        results[0].Errors.Should().ContainSingle().Which.Code.Should().Be(ValidationFailedCode);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnBadRequest_When_CustomerBatchExceedsLimit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        IReadOnlyList<CustomerBulkRow> rows = BuildRows(MaxRows + 1);

        using HttpResponseMessage response = await BulkTestClient.PostAsync(
            client,
            BulkTestClient.CustomersBulkRoute,
            rows,
            false,
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnBadRequest_When_ApproverBatchExceedsLimit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersApiFactory factory = await CustomersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        IReadOnlyList<ApproverBulkRow> rows = BuildApproverRows(MaxRows + 1);

        using HttpResponseMessage response = await BulkTestClient.PostAsync(
            client,
            BulkTestClient.ApproversBulkRoute,
            rows,
            false,
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static CustomerBulkRow BuildRow(int rowNumber, string taxId) =>
        new(rowNumber, taxId, GeneratedName, GeneratedEmail, GeneratedPhone);

    private static IReadOnlyList<CustomerBulkRow> BuildRows(int count) =>
        Enumerable.Range(FirstRowNumber, count)
            .Select(rowNumber => BuildRow(rowNumber, (FirstGeneratedTaxId + rowNumber).ToString(CultureInfo.InvariantCulture)))
            .ToList();

    private static IReadOnlyList<ApproverBulkRow> BuildApproverRows(int count) =>
        Enumerable.Range(FirstRowNumber, count)
            .Select(rowNumber => new ApproverBulkRow(rowNumber, RepeatedTaxId, GeneratedName, GeneratedEmail))
            .ToList();
}
