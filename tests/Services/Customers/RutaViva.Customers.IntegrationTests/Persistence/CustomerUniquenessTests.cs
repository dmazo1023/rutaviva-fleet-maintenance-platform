using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;
using RutaViva.Customers.Infrastructure.Persistence;
using RutaViva.Customers.Infrastructure.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class CustomerUniquenessTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string DuplicatedTaxId = "900100200-1";
    private const string ApproverTaxId = "900100300-2";
    private const string CheckerTaxId = "900100400-3";
    private const string UnknownTaxId = "900100500-4";
    private const string FirstEmail = "first@acme.test";
    private const string SecondEmail = "second@acme.test";
    private const string ApproverName = "Maria Lopez";
    private const string ApproverEmail = "approver@acme.test";

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnTaxIdAlreadyRegistered_When_SecondCustomerRepeatsTaxId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersDbContext context = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        CustomerRepository customers = new(context);
        CustomersUnitOfWork unitOfWork = new(context);
        customers.Add(CustomerTestData.CreateCustomer(DuplicatedTaxId, FirstEmail));
        Result first = await unitOfWork.SaveChangesAsync(cancellationToken);
        customers.Add(CustomerTestData.CreateCustomer(DuplicatedTaxId, SecondEmail));

        Result second = await unitOfWork.SaveChangesAsync(cancellationToken);

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Should().Be(CustomerErrors.TaxIdAlreadyRegistered);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnApproverEmailAlreadyRegistered_When_StaleCustomerRepeatsApproverEmail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Customer customer = CustomerTestData.CreateCustomer(ApproverTaxId, FirstEmail);
        await SaveCustomerWithApproverAsync(customer, cancellationToken);
        await using CustomersDbContext staleContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        CustomerId customerId = customer.Id;
        Customer stale = await staleContext.Customers.FirstAsync(candidate => candidate.Id == customerId, cancellationToken);
        Result<ApproverId> added = stale.AddApprover(
            CustomerTestData.Name(ApproverName),
            CustomerTestData.Email(ApproverEmail),
            CustomerTestData.OccurredAt);

        Result saved = await new CustomersUnitOfWork(staleContext).SaveChangesAsync(cancellationToken);

        added.IsSuccess.Should().BeTrue();
        saved.IsFailure.Should().BeTrue();
        saved.Error.Should().Be(CustomerErrors.ApproverEmailAlreadyRegistered);
    }

    [Fact]
    public async Task IsTaxIdTakenAsync_Should_ReturnTrueOnlyForStoredTaxId_When_CustomerIsRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersDbContext writeContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        new CustomerRepository(writeContext).Add(CustomerTestData.CreateCustomer(CheckerTaxId, FirstEmail));
        await new CustomersUnitOfWork(writeContext).SaveChangesAsync(cancellationToken);
        await using CustomersDbContext readContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        CustomerUniquenessChecker checker = new(readContext);

        bool stored = await checker.IsTaxIdTakenAsync(TaxId.Create(CheckerTaxId).Value, cancellationToken);
        bool unknown = await checker.IsTaxIdTakenAsync(TaxId.Create(UnknownTaxId).Value, cancellationToken);

        stored.Should().BeTrue();
        unknown.Should().BeFalse();
    }

    private async Task SaveCustomerWithApproverAsync(Customer customer, CancellationToken cancellationToken)
    {
        await using CustomersDbContext context = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        customer.AddApprover(
            CustomerTestData.Name(ApproverName),
            CustomerTestData.Email(ApproverEmail),
            CustomerTestData.OccurredAt);
        new CustomerRepository(context).Add(customer);
        Result saved = await new CustomersUnitOfWork(context).SaveChangesAsync(cancellationToken);
        saved.IsSuccess.Should().BeTrue();
    }
}
