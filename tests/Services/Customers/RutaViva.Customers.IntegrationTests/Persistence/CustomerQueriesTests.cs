using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Infrastructure.Persistence;
using RutaViva.Customers.Infrastructure.Persistence.Queries;
using RutaViva.Customers.Infrastructure.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class CustomerQueriesTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string DetailTaxId = "900200100-1";
    private const string ActiveTaxId = "900200200-2";
    private const string InactiveTaxId = "900200300-3";
    private const string CustomerEmail = "customer@acme.test";
    private const string FirstApproverName = "Ana Torres";
    private const string FirstApproverEmail = "ana@acme.test";
    private const string SecondApproverName = "Luis Perez";
    private const string SecondApproverEmail = "luis@acme.test";
    private const int ApproverCount = 2;

    [Fact]
    public async Task GetDetailAsync_Should_ReturnApprovers_When_CustomerHasApprovers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Customer customer = CustomerTestData.CreateCustomer(DetailTaxId, CustomerEmail);
        customer.AddApprover(CustomerTestData.Name(FirstApproverName), CustomerTestData.Email(FirstApproverEmail), CustomerTestData.OccurredAt);
        customer.AddApprover(CustomerTestData.Name(SecondApproverName), CustomerTestData.Email(SecondApproverEmail), CustomerTestData.OccurredAt);
        await SaveAsync(customer, cancellationToken);
        await using CustomersDbContext readContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);

        CustomerDetailDto? detail = await new CustomerQueries(readContext).GetDetailAsync(customer.Id, cancellationToken);

        detail.Should().NotBeNull();
        detail!.TaxId.Should().Be(DetailTaxId);
        detail.Status.Should().Be(CustomerStatus.Active);
        detail.Approvers.Should().HaveCount(ApproverCount);
        detail.Approvers.Select(approver => approver.Email).Should().BeEquivalentTo([FirstApproverEmail, SecondApproverEmail]);
        detail.Approvers.Should().OnlyContain(approver => approver.Status == ApproverStatus.Active);
    }

    [Fact]
    public async Task GetDetailAsync_Should_ReturnNull_When_CustomerDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersDbContext readContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);

        CustomerDetailDto? detail = await new CustomerQueries(readContext).GetDetailAsync(CustomerId.New(), cancellationToken);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_Should_ReturnOnlyMatchingStatus_When_StatusFilterIsProvided()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Customer active = CustomerTestData.CreateCustomer(ActiveTaxId, CustomerEmail);
        Customer inactive = CustomerTestData.CreateCustomer(InactiveTaxId, CustomerEmail);
        inactive.Deactivate(CustomerTestData.OccurredAt).IsSuccess.Should().BeTrue();
        await SaveAsync(active, cancellationToken);
        await SaveAsync(inactive, cancellationToken);
        await using CustomersDbContext readContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);

        IReadOnlyList<CustomerSummaryDto> inactiveOnly = await new CustomerQueries(readContext)
            .ListAsync(CustomerStatus.Inactive, cancellationToken);
        IReadOnlyList<CustomerSummaryDto> all = await new CustomerQueries(readContext).ListAsync(null, cancellationToken);

        inactiveOnly.Should().Contain(summary => summary.TaxId == InactiveTaxId);
        inactiveOnly.Should().NotContain(summary => summary.TaxId == ActiveTaxId);
        all.Should().Contain(summary => summary.TaxId == InactiveTaxId);
        all.Should().Contain(summary => summary.TaxId == ActiveTaxId);
    }

    private async Task SaveAsync(Customer customer, CancellationToken cancellationToken)
    {
        await using CustomersDbContext context = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        new CustomerRepository(context).Add(customer);
        Result saved = await new CustomersUnitOfWork(context).SaveChangesAsync(cancellationToken);
        saved.IsSuccess.Should().BeTrue();
    }
}
