using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.Application.UnitTests.Customers;

public sealed class RegisterCustomerHandlerTests
{
    private const string InvalidTaxId = "abc";
    private const string InvalidPhone = "12";
    private const string InvalidName = " ";

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerUniquenessChecker _uniqueness = Substitute.For<ICustomerUniquenessChecker>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(CustomerTestFactory.OccurredAt());
    private readonly RegisterCustomerHandler _handler;

    public RegisterCustomerHandlerTests()
    {
        _uniqueness.IsTaxIdTakenAsync(Arg.Any<TaxId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new RegisterCustomerHandler(_customers, _uniqueness, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_AddActiveCustomerAndSave_When_TaxIdIsFree()
    {
        Customer? added = null;
        _customers.When(repository => repository.Add(Arg.Any<Customer>())).Do(call => added = call.Arg<Customer>());

        Result<Guid> result = await _handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        added!.Id.Value.Should().Be(result.Value);
        added.Status.Should().Be(CustomerStatus.Active);
        added.TaxId.Value.Should().Be(CustomerTestFactory.TaxIdText);
        added.Phone!.Value.Should().Be(CustomerTestFactory.PhoneText);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_RegisterWithoutPhone_When_PhoneIsNotProvided()
    {
        Customer? added = null;
        _customers.When(repository => repository.Add(Arg.Any<Customer>())).Do(call => added = call.Arg<Customer>());
        RegisterCustomerCommand command = Command() with { Phone = null };

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        added!.Phone.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTaxIdAlreadyRegistered_When_TaxIdIsTaken()
    {
        _uniqueness.IsTaxIdTakenAsync(Arg.Any<TaxId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        Result<Guid> result = await _handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.TaxIdAlreadyRegistered);
        _customers.DidNotReceive().Add(Arg.Any<Customer>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTaxIdInvalid_When_TaxIdHasInvalidFormat()
    {
        RegisterCustomerCommand command = Command() with { TaxId = InvalidTaxId };

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.TaxIdInvalid);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnPhoneNumberInvalid_When_PhoneHasInvalidFormat()
    {
        RegisterCustomerCommand command = Command() with { Phone = InvalidPhone };

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.PhoneNumberInvalid);
        _customers.DidNotReceive().Add(Arg.Any<Customer>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNameRequired_When_NameIsBlank()
    {
        RegisterCustomerCommand command = Command() with { Name = InvalidName };

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.PersonNameRequired);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSaveError_When_SaveFails()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(CustomerErrors.TaxIdAlreadyRegistered)));

        Result<Guid> result = await _handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(CustomerErrors.TaxIdAlreadyRegistered);
    }

    private static RegisterCustomerCommand Command() =>
        new(CustomerTestFactory.CustomerName, CustomerTestFactory.TaxIdText, CustomerTestFactory.CustomerEmail, CustomerTestFactory.PhoneText);
}
