using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.UnitTests.Workshops;

public sealed class RegisterWorkshopHandlerTests
{
    private const string BlankValue = "   ";

    private readonly IWorkshopRepository _workshops = Substitute.For<IWorkshopRepository>();
    private readonly IWorkshopUniquenessChecker _uniqueness = Substitute.For<IWorkshopUniquenessChecker>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterWorkshopHandler _handler;

    public RegisterWorkshopHandlerTests()
    {
        _uniqueness.IsTaxIdTakenAsync(Arg.Any<TaxId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new RegisterWorkshopHandler(_workshops, _uniqueness, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_AddWorkshopAndSave_When_DataIsValid()
    {
        Result<Guid> result = await _handler.HandleAsync(WorkshopTestFactory.Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _workshops.Received(1).Add(Arg.Is<Workshop>(workshop =>
            workshop.Id.Value == result.Value && workshop.Status == WorkshopStatus.Active));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_RegisterWorkshopWithoutPhone_When_PhoneIsBlank()
    {
        Result<Guid> result = await _handler.HandleAsync(
            WorkshopTestFactory.Command(phone: BlankValue),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _workshops.Received(1).Add(Arg.Is<Workshop>(workshop => workshop.Phone == null));
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTaxIdAlreadyRegistered_When_TaxIdIsTaken()
    {
        _uniqueness.IsTaxIdTakenAsync(Arg.Any<TaxId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        Result<Guid> result = await _handler.HandleAsync(WorkshopTestFactory.Command(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(WorkshopErrors.TaxIdAlreadyRegistered);
        _workshops.DidNotReceive().Add(Arg.Any<Workshop>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTaxIdInvalid_When_TaxIdIsMalformed()
    {
        Result<Guid> result = await _handler.HandleAsync(
            WorkshopTestFactory.Command(taxId: BlankValue),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(WorkshopErrors.TaxIdInvalid);
        _workshops.DidNotReceive().Add(Arg.Any<Workshop>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnAddressRequired_When_AddressIsBlank()
    {
        Result<Guid> result = await _handler.HandleAsync(
            WorkshopTestFactory.Command(address: BlankValue),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(WorkshopErrors.AddressRequired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSaveError_When_SaveFails()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(WorkshopErrors.TaxIdAlreadyRegistered)));

        Result<Guid> result = await _handler.HandleAsync(WorkshopTestFactory.Command(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(WorkshopErrors.TaxIdAlreadyRegistered);
    }
}
