using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Approvers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.Events;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.Application.UnitTests.Approvers;

public sealed class ChangeApproverStatusHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(CustomerTestFactory.OccurredAt());
    private readonly ChangeApproverStatusHandler _handler;

    public ChangeApproverStatusHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new ChangeApproverStatusHandler(_customers, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_DeactivateApproverAndSave_When_TargetIsInactive()
    {
        Customer customer = StubCustomer(CustomerTestFactory.CustomerWithApprover());
        Approver approver = customer.Approvers.Single();
        ChangeApproverStatusCommand command = new(customer.Id, approver.Id, ApproverStatus.Inactive);

        Result<ApproverId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Value.Should().Be(approver.Id);
        approver.Status.Should().Be(ApproverStatus.Inactive);
        customer.DomainEvents.OfType<ApproverStatusChanged>().Should().ContainSingle();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_NotRaiseEvent_When_ApproverAlreadyHasTargetStatus()
    {
        Customer customer = StubCustomer(CustomerTestFactory.CustomerWithApprover());
        ChangeApproverStatusCommand command = new(customer.Id, customer.Approvers.Single().Id, ApproverStatus.Active);

        Result<ApproverId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        customer.DomainEvents.OfType<ApproverStatusChanged>().Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnApproverNotFound_When_ApproverDoesNotBelongToCustomer()
    {
        Customer customer = StubCustomer(CustomerTestFactory.ActiveCustomer());
        ChangeApproverStatusCommand command = new(customer.Id, ApproverId.New(), ApproverStatus.Inactive);

        Result<ApproverId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.ApproverNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_CustomerDoesNotExist()
    {
        ChangeApproverStatusCommand command = new(CustomerId.New(), ApproverId.New(), ApproverStatus.Inactive);
        _customers.GetByIdAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Customer?>(null));

        Result<ApproverId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.NotFound);
    }

    private Customer StubCustomer(Customer customer)
    {
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Customer?>(customer));
        return customer;
    }
}
