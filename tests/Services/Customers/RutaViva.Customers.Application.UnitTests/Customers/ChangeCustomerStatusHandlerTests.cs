using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.Events;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.Application.UnitTests.Customers;

public sealed class ChangeCustomerStatusHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(CustomerTestFactory.OccurredAt());
    private readonly ChangeCustomerStatusHandler _handler;

    public ChangeCustomerStatusHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new ChangeCustomerStatusHandler(_customers, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_DeactivateCustomerAndSave_When_TargetIsInactive()
    {
        Customer customer = StubCustomer();
        ChangeCustomerStatusCommand command = new(customer.Id, CustomerStatus.Inactive);

        Result<CustomerId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Value.Should().Be(customer.Id);
        customer.Status.Should().Be(CustomerStatus.Inactive);
        customer.DomainEvents.OfType<CustomerStatusChanged>().Should().ContainSingle();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_NotRaiseEvent_When_StatusIsUnchanged()
    {
        Customer customer = StubCustomer();
        ChangeCustomerStatusCommand command = new(customer.Id, CustomerStatus.Active);

        Result<CustomerId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        customer.DomainEvents.OfType<CustomerStatusChanged>().Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_CustomerDoesNotExist()
    {
        ChangeCustomerStatusCommand command = new(CustomerId.New(), CustomerStatus.Inactive);
        _customers.GetByIdAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Customer?>(null));

        Result<CustomerId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Customer StubCustomer()
    {
        Customer customer = CustomerTestFactory.ActiveCustomer();
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Customer?>(customer));
        return customer;
    }
}
