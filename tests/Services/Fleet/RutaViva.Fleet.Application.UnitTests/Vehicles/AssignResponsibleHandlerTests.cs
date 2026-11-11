using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class AssignResponsibleHandlerTests
{
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IFleetReferenceReader _references = Substitute.For<IFleetReferenceReader>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(VehicleTestFactory.Later);
    private readonly AssignResponsibleHandler _handler;

    public AssignResponsibleHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new AssignResponsibleHandler(_vehicles, _references, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_AssignResponsibleAndSave_When_ApproverExists()
    {
        Vehicle vehicle = StubVehicle();
        ApproverReference responsible = StubApprover(VehicleTestFactory.Approver(CustomerId.New()));
        AssignResponsibleCommand command = new(vehicle.Id, responsible.Id);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        vehicle.ResponsibleApproverId.Should().Be(responsible.Id);
        vehicle.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<VehicleResponsibleAssigned>();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_NotEmitEvent_When_ResponsibleIsTheSame()
    {
        ApproverReference responsible = VehicleTestFactory.Approver(CustomerId.New());
        Vehicle vehicle = StubVehicle(responsible);
        StubApprover(responsible);
        AssignResponsibleCommand command = new(vehicle.Id, responsible.Id);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        vehicle.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_RemoveResponsible_When_NoApproverIsGiven()
    {
        Vehicle vehicle = StubVehicle(VehicleTestFactory.Approver(CustomerId.New()));
        AssignResponsibleCommand command = new(vehicle.Id, null);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        vehicle.ResponsibleApproverId.Should().BeNull();
        vehicle.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<VehicleResponsibleAssigned>();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnResponsibleNotFound_When_ApproverReplicaIsMissing()
    {
        Vehicle vehicle = StubVehicle();
        ApproverId missing = ApproverId.New();
        _references.FindApproverAsync(missing, Arg.Any<CancellationToken>()).Returns(Task.FromResult<ApproverReference?>(null));
        AssignResponsibleCommand command = new(vehicle.Id, missing);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.ResponsibleNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnResponsibleInactive_When_ApproverIsInactive()
    {
        Vehicle vehicle = StubVehicle();
        ApproverReference responsible = StubApprover(VehicleTestFactory.Approver(CustomerId.New(), ReferenceStatus.Inactive));
        AssignResponsibleCommand command = new(vehicle.Id, responsible.Id);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleErrors.ResponsibleInactive);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnVehicleNotFound_When_VehicleDoesNotExist()
    {
        AssignResponsibleCommand command = new(VehicleId.New(), null);
        _vehicles.GetByIdAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(null));

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(VehicleApplicationErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Vehicle StubVehicle(ApproverReference? responsible = null)
    {
        Vehicle vehicle = VehicleTestFactory.RegisteredVehicle(responsible);
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(vehicle));
        return vehicle;
    }

    private ApproverReference StubApprover(ApproverReference approver)
    {
        _references.FindApproverAsync(approver.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<ApproverReference?>(approver));
        return approver;
    }
}
