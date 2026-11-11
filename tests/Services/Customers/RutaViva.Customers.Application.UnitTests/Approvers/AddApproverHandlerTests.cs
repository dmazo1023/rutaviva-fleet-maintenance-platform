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

public sealed class AddApproverHandlerTests
{
    private const string InvalidEmail = "not-an-email";

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(CustomerTestFactory.OccurredAt());
    private readonly AddApproverHandler _handler;

    public AddApproverHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new AddApproverHandler(_customers, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_AddApproverToAggregateAndSave_When_CustomerExists()
    {
        Customer customer = StubCustomer(CustomerTestFactory.ActiveCustomer());
        AddApproverCommand command = new(customer.Id, CustomerTestFactory.ApproverName, CustomerTestFactory.ApproverEmail);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Approver approver = customer.Approvers.Single();
        result.Value.Should().Be(approver.Id.Value);
        approver.Email.Value.Should().Be(CustomerTestFactory.ApproverEmail);
        approver.Status.Should().Be(ApproverStatus.Active);
        customer.DomainEvents.OfType<ApproverRegistered>().Should().ContainSingle();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_CustomerDoesNotExist()
    {
        AddApproverCommand command = new(CustomerId.New(), CustomerTestFactory.ApproverName, CustomerTestFactory.ApproverEmail);
        _customers.GetByIdAsync(command.CustomerId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Customer?>(null));

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnEmailAlreadyRegistered_When_EmailExistsInCustomer()
    {
        Customer customer = StubCustomer(CustomerTestFactory.CustomerWithApprover());
        AddApproverCommand command = new(customer.Id, CustomerTestFactory.ApproverName, CustomerTestFactory.ApproverEmail);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.ApproverEmailAlreadyRegistered);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInactive_When_CustomerIsInactive()
    {
        Customer customer = CustomerTestFactory.ActiveCustomer();
        customer.Deactivate(CustomerTestFactory.OccurredAt());
        StubCustomer(customer);
        AddApproverCommand command = new(customer.Id, CustomerTestFactory.ApproverName, CustomerTestFactory.ApproverEmail);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.Inactive);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnEmailInvalid_When_EmailHasInvalidFormat()
    {
        Customer customer = StubCustomer(CustomerTestFactory.ActiveCustomer());
        AddApproverCommand command = new(customer.Id, CustomerTestFactory.ApproverName, InvalidEmail);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.EmailAddressInvalid);
        customer.Approvers.Should().BeEmpty();
    }

    private Customer StubCustomer(Customer customer)
    {
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Customer?>(customer));
        return customer;
    }
}
