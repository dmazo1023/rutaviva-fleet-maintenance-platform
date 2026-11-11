using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Application.Customers.Queries;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.UnitTests.Customers;

public sealed class CustomerQueryHandlerTests
{
    private readonly ICustomerQueries _queries = Substitute.For<ICustomerQueries>();

    [Fact]
    public async Task ListCustomersHandleAsync_Should_ReturnSummaries_When_StatusFilterIsGiven()
    {
        CustomerSummaryDto summary = new(
            Guid.NewGuid(),
            CustomerTestFactory.CustomerName,
            CustomerTestFactory.TaxIdText,
            CustomerTestFactory.CustomerEmail,
            CustomerStatus.Active);
        _queries.ListAsync(CustomerStatus.Active, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CustomerSummaryDto>>([summary]));
        ListCustomersHandler handler = new(_queries);

        Result<IReadOnlyList<CustomerSummaryDto>> result =
            await handler.HandleAsync(new ListCustomersQuery(CustomerStatus.Active), TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle().Which.Should().Be(summary);
    }

    [Fact]
    public async Task GetCustomerDetailHandleAsync_Should_ReturnDetailWithApprovers_When_CustomerExists()
    {
        CustomerId id = CustomerId.New();
        ApproverDto approver = new(Guid.NewGuid(), CustomerTestFactory.ApproverName, CustomerTestFactory.ApproverEmail, ApproverStatus.Active);
        CustomerDetailDto detail = new(
            id.Value,
            CustomerTestFactory.CustomerName,
            CustomerTestFactory.TaxIdText,
            CustomerTestFactory.CustomerEmail,
            CustomerTestFactory.PhoneText,
            CustomerStatus.Active,
            [approver]);
        _queries.GetDetailAsync(id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<CustomerDetailDto?>(detail));
        GetCustomerDetailHandler handler = new(_queries);

        Result<CustomerDetailDto> result =
            await handler.HandleAsync(new GetCustomerDetailQuery(id), TestContext.Current.CancellationToken);

        result.Value.Approvers.Should().ContainSingle().Which.Should().Be(approver);
    }

    [Fact]
    public async Task GetCustomerDetailHandleAsync_Should_ReturnNotFound_When_CustomerDoesNotExist()
    {
        CustomerId id = CustomerId.New();
        _queries.GetDetailAsync(id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<CustomerDetailDto?>(null));
        GetCustomerDetailHandler handler = new(_queries);

        Result<CustomerDetailDto> result =
            await handler.HandleAsync(new GetCustomerDetailQuery(id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.NotFound);
    }
}
