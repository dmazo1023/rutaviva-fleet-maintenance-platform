using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class RegisterVehicleHandlerTests
{
    private const string MalformedPlate = "RV-107";

    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IVehicleUniquenessChecker _uniqueness = Substitute.For<IVehicleUniquenessChecker>();
    private readonly IFleetReferenceReader _references = Substitute.For<IFleetReferenceReader>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(VehicleTestFactory.Now);
    private readonly CustomerReference _owner = VehicleTestFactory.Owner();
    private readonly WorkshopReference _workshop = VehicleTestFactory.Workshop();
    private readonly RegisterVehicleHandler _handler;

    public RegisterVehicleHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        StubOwner(_owner);
        StubWorkshop(_workshop);
        _handler = new RegisterVehicleHandler(_vehicles, _uniqueness, _references, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_RegisterVehicleAndSave_When_CommandIsValid()
    {
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _vehicles.Received(1).Add(Arg.Is<Vehicle>(vehicle => vehicle.Id.Value == result.Value && vehicle.Plate.Value == VehicleTestFactory.PlateText));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_AcceptResponsible_When_ApproverReplicaExists()
    {
        ApproverReference responsible = VehicleTestFactory.Approver(_owner.Id);
        _references.FindApproverAsync(responsible.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<ApproverReference?>(responsible));
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id, responsible.Id);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _vehicles.Received(1).Add(Arg.Is<Vehicle>(vehicle => vehicle.ResponsibleApproverId == responsible.Id));
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnOwnerNotFound_When_OwnerReplicaIsMissing()
    {
        StubOwner(null);
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.OwnerNotFound);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnWorkshopNotFound_When_WorkshopReplicaIsMissing()
    {
        StubWorkshop(null);
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.WorkshopNotFound);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnResponsibleNotFound_When_ApproverReplicaIsMissing()
    {
        ApproverId missing = ApproverId.New();
        _references.FindApproverAsync(missing, Arg.Any<CancellationToken>()).Returns(Task.FromResult<ApproverReference?>(null));
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id, missing);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.ResponsibleNotFound);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnOwnerInactive_When_OwnerReplicaIsInactive()
    {
        StubOwner(VehicleTestFactory.Owner(ReferenceStatus.Inactive));
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.OwnerInactive);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnPlateAlreadyRegistered_When_PlateIsTaken()
    {
        _uniqueness.IsPlateTakenAsync(Arg.Any<Plate>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.PlateAlreadyRegistered);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnVinAlreadyRegistered_When_VinIsTaken()
    {
        _uniqueness.IsVinTakenAsync(Arg.Any<Vin>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.VinAlreadyRegistered);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_SkipVinCheck_When_VinIsNotProvided()
    {
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id, vin: null);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _uniqueness.DidNotReceive().IsVinTakenAsync(Arg.Any<Vin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidPlate_When_PlateIsMalformed()
    {
        RegisterVehicleCommand command = VehicleTestFactory.Command(_owner.Id, _workshop.Id, plate: MalformedPlate);

        Result<Guid> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.InvalidPlate);
        await AssertNothingPersistedAsync();
    }

    private void StubOwner(CustomerReference? owner) =>
        _references.FindCustomerAsync(_owner.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult(owner));

    private void StubWorkshop(WorkshopReference? workshop) =>
        _references.FindWorkshopAsync(_workshop.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult(workshop));

    private async Task AssertNothingPersistedAsync()
    {
        _vehicles.DidNotReceive().Add(Arg.Any<Vehicle>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
