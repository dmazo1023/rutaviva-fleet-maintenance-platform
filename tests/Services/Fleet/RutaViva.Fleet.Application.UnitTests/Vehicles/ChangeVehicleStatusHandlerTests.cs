using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class ChangeVehicleStatusHandlerTests
{
    private const int UndefinedStatus = 0;

    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(VehicleTestFactory.Later);
    private readonly ChangeVehicleStatusHandler _handler;

    public ChangeVehicleStatusHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new ChangeVehicleStatusHandler(_vehicles, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_DeactivateAndEmitEvent_When_TargetIsInactive()
    {
        Vehicle vehicle = StubVehicle();
        ChangeVehicleStatusCommand command = new(vehicle.Id, VehicleStatus.Inactive);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        vehicle.Status.Should().Be(VehicleStatus.Inactive);
        vehicle.StatusChangedAt.Should().Be(VehicleTestFactory.Later);
        vehicle.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<VehicleStatusChanged>();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReactivateAndEmitEvent_When_TargetIsActive()
    {
        Vehicle vehicle = StubVehicle();
        await _handler.HandleAsync(new ChangeVehicleStatusCommand(vehicle.Id, VehicleStatus.Inactive), TestContext.Current.CancellationToken);
        ((IAggregateRoot)vehicle).ClearDomainEvents();
        ChangeVehicleStatusCommand command = new(vehicle.Id, VehicleStatus.Active);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        vehicle.Status.Should().Be(VehicleStatus.Active);
        vehicle.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<VehicleStatusChanged>();
    }

    [Fact]
    public async Task HandleAsync_Should_NotEmitEvent_When_TargetIsCurrentStatus()
    {
        Vehicle vehicle = StubVehicle();
        ChangeVehicleStatusCommand command = new(vehicle.Id, VehicleStatus.Active);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(vehicle.Id);
        vehicle.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnVehicleNotFound_When_VehicleDoesNotExist()
    {
        ChangeVehicleStatusCommand command = new(VehicleId.New(), VehicleStatus.Inactive);
        _vehicles.GetByIdAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(null));

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleApplicationErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidStatus_When_TargetIsUndefined()
    {
        Vehicle vehicle = StubVehicle();
        ChangeVehicleStatusCommand command = new(vehicle.Id, (VehicleStatus)UndefinedStatus);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleApplicationErrors.InvalidStatus);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Vehicle StubVehicle()
    {
        Vehicle vehicle = VehicleTestFactory.RegisteredVehicle();
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(vehicle));
        return vehicle;
    }
}
