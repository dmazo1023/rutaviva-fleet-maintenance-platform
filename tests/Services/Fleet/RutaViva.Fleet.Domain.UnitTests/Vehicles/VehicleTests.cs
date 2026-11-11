using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.Events;

namespace RutaViva.Fleet.Domain.UnitTests.Vehicles;

public sealed class VehicleTests
{
    private const string OwnerInactiveCode = "Vehicle.OwnerInactive";
    private const string WorkshopInactiveCode = "Vehicle.WorkshopInactive";
    private const string ResponsibleInactiveCode = "Vehicle.ResponsibleInactive";

    [Fact]
    public void Register_Should_ReturnActiveVehicleAndRaiseRegistered_When_ReferencesAreActive()
    {
        CustomerReference owner = VehicleTestData.Owner();
        WorkshopReference workshop = VehicleTestData.Workshop();
        ApproverReference responsible = VehicleTestData.Approver(owner.Id);

        Result<Vehicle> result = Register(owner, workshop, responsible);

        result.IsSuccess.Should().BeTrue();
        Vehicle vehicle = result.Value;
        vehicle.Status.Should().Be(VehicleStatus.Active);
        vehicle.Plate.Value.Should().Be(VehicleTestData.PlateText);
        vehicle.Vin!.Value.Should().Be(VehicleTestData.VinText);
        vehicle.CustomerId.Should().Be(owner.Id);
        vehicle.WorkshopId.Should().Be(workshop.Id);
        vehicle.ResponsibleApproverId.Should().Be(responsible.Id);
        vehicle.InitialOdometer.Value.Should().Be(VehicleTestData.OdometerKm);
        vehicle.RegisteredAt.Should().Be(VehicleTestData.Now);
        vehicle.StatusChangedAt.Should().Be(VehicleTestData.Now);
        VehicleRegistered raised = vehicle.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<VehicleRegistered>().Subject;
        raised.VehicleId.Should().Be(vehicle.Id);
        raised.CustomerId.Should().Be(owner.Id);
        raised.ResponsibleApproverId.Should().Be(responsible.Id);
        raised.Status.Should().Be(VehicleStatus.Active);
        raised.InitialOdometer.Value.Should().Be(VehicleTestData.OdometerKm);
        raised.OccurredAt.Should().Be(VehicleTestData.Now);
    }

    [Fact]
    public void Register_Should_Succeed_When_ResponsibleIsOmitted()
    {
        Result<Vehicle> result = Register(VehicleTestData.Owner(), VehicleTestData.Workshop(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResponsibleApproverId.Should().BeNull();
    }

    [Fact]
    public void Register_Should_Succeed_When_VinIsOmitted()
    {
        Result<Vehicle> result = Vehicle.Register(
            VehicleTestData.NewPlate(),
            null,
            VehicleTestData.NewDescription(),
            VehicleTestData.NewOdometer(),
            VehicleTestData.Owner(),
            VehicleTestData.Workshop(),
            null,
            VehicleTestData.Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Vin.Should().BeNull();
    }

    [Fact]
    public void Register_Should_ReturnOwnerInactive_When_OwnerIsInactive()
    {
        Result<Vehicle> result = Register(VehicleTestData.Owner(ReferenceStatus.Inactive), VehicleTestData.Workshop(), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OwnerInactiveCode);
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public void Register_Should_ReturnWorkshopInactive_When_WorkshopIsInactive()
    {
        Result<Vehicle> result = Register(VehicleTestData.Owner(), VehicleTestData.Workshop(ReferenceStatus.Inactive), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(WorkshopInactiveCode);
    }

    [Fact]
    public void Register_Should_ReturnResponsibleInactive_When_ResponsibleIsInactive()
    {
        CustomerReference owner = VehicleTestData.Owner();
        ApproverReference responsible = VehicleTestData.Approver(owner.Id, ReferenceStatus.Inactive);

        Result<Vehicle> result = Register(owner, VehicleTestData.Workshop(), responsible);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ResponsibleInactiveCode);
    }

    [Fact]
    public void Register_Should_AcceptResponsible_When_ApproverBelongsToAnotherCustomer()
    {
        CustomerReference owner = VehicleTestData.Owner();
        CustomerReference otherCustomer = VehicleTestData.Owner();
        ApproverReference responsible = VehicleTestData.Approver(otherCustomer.Id);

        Result<Vehicle> result = Register(owner, VehicleTestData.Workshop(), responsible);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResponsibleApproverId.Should().Be(responsible.Id);
    }

    [Fact]
    public void AssignResponsible_Should_RaiseAssigned_When_ResponsibleChanges()
    {
        Vehicle vehicle = RegisteredVehicle();
        ClearEvents(vehicle);
        ApproverReference responsible = VehicleTestData.Approver(vehicle.CustomerId);

        Result result = vehicle.AssignResponsible(responsible, VehicleTestData.Later);

        result.IsSuccess.Should().BeTrue();
        vehicle.ResponsibleApproverId.Should().Be(responsible.Id);
        VehicleResponsibleAssigned raised = vehicle.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<VehicleResponsibleAssigned>().Subject;
        raised.VehicleId.Should().Be(vehicle.Id);
        raised.ResponsibleApproverId.Should().Be(responsible.Id);
        raised.OccurredAt.Should().Be(VehicleTestData.Later);
    }

    [Fact]
    public void AssignResponsible_Should_NotRaiseEvent_When_ResponsibleIsTheSame()
    {
        Vehicle vehicle = RegisteredVehicle();
        ApproverReference responsible = VehicleTestData.Approver(vehicle.CustomerId);
        vehicle.AssignResponsible(responsible, VehicleTestData.Later);
        ClearEvents(vehicle);

        Result result = vehicle.AssignResponsible(responsible, VehicleTestData.Later);

        result.IsSuccess.Should().BeTrue();
        vehicle.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignResponsible_Should_RaiseAssignedWithoutResponsible_When_ResponsibleIsRemoved()
    {
        Vehicle vehicle = RegisteredVehicle();
        vehicle.AssignResponsible(VehicleTestData.Approver(vehicle.CustomerId), VehicleTestData.Later);
        ClearEvents(vehicle);

        Result result = vehicle.AssignResponsible(null, VehicleTestData.Later);

        result.IsSuccess.Should().BeTrue();
        vehicle.ResponsibleApproverId.Should().BeNull();
        VehicleResponsibleAssigned raised = vehicle.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<VehicleResponsibleAssigned>().Subject;
        raised.ResponsibleApproverId.Should().BeNull();
    }

    [Fact]
    public void AssignResponsible_Should_NotRaiseEvent_When_VehicleHasNoResponsibleAndNullIsAssigned()
    {
        Vehicle vehicle = RegisteredVehicle();
        ClearEvents(vehicle);

        Result result = vehicle.AssignResponsible(null, VehicleTestData.Later);

        result.IsSuccess.Should().BeTrue();
        vehicle.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignResponsible_Should_ReturnResponsibleInactive_When_ApproverIsInactive()
    {
        Vehicle vehicle = RegisteredVehicle();
        ClearEvents(vehicle);
        ApproverReference inactive = VehicleTestData.Approver(vehicle.CustomerId, ReferenceStatus.Inactive);

        Result result = vehicle.AssignResponsible(inactive, VehicleTestData.Later);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ResponsibleInactiveCode);
        vehicle.ResponsibleApproverId.Should().BeNull();
        vehicle.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignResponsible_Should_RaiseAssigned_When_UsingTheOverloadWithoutClock()
    {
        Vehicle vehicle = RegisteredVehicle();
        ClearEvents(vehicle);

        Result result = vehicle.AssignResponsible(VehicleTestData.Approver(vehicle.CustomerId));

        result.IsSuccess.Should().BeTrue();
        vehicle.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<VehicleResponsibleAssigned>();
    }

    [Fact]
    public void Deactivate_Should_RaiseStatusChangedOnce_When_CalledTwice()
    {
        Vehicle vehicle = RegisteredVehicle();
        ClearEvents(vehicle);

        Result first = vehicle.Deactivate(VehicleTestData.Later);
        Result second = vehicle.Deactivate(VehicleTestData.Later.AddHours(1));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        vehicle.Status.Should().Be(VehicleStatus.Inactive);
        vehicle.StatusChangedAt.Should().Be(VehicleTestData.Later);
        VehicleStatusChanged raised = vehicle.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<VehicleStatusChanged>().Subject;
        raised.Status.Should().Be(VehicleStatus.Inactive);
        raised.Plate.Should().Be(vehicle.Plate);
        raised.CustomerId.Should().Be(vehicle.CustomerId);
        raised.OccurredAt.Should().Be(VehicleTestData.Later);
    }

    [Fact]
    public void Reactivate_Should_RaiseStatusChanged_When_VehicleIsInactive()
    {
        Vehicle vehicle = RegisteredVehicle();
        vehicle.Deactivate(VehicleTestData.Later);
        ClearEvents(vehicle);

        Result result = vehicle.Reactivate(VehicleTestData.Later.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        vehicle.Status.Should().Be(VehicleStatus.Active);
        VehicleStatusChanged raised = vehicle.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<VehicleStatusChanged>().Subject;
        raised.Status.Should().Be(VehicleStatus.Active);
    }

    [Fact]
    public void Reactivate_Should_NotRaiseEvent_When_VehicleIsAlreadyActive()
    {
        Vehicle vehicle = RegisteredVehicle();
        ClearEvents(vehicle);

        Result result = vehicle.Reactivate(VehicleTestData.Later);

        result.IsSuccess.Should().BeTrue();
        vehicle.StatusChangedAt.Should().Be(VehicleTestData.Now);
        vehicle.DomainEvents.Should().BeEmpty();
    }

    private static Result<Vehicle> Register(CustomerReference owner, WorkshopReference workshop, ApproverReference? responsible) =>
        Vehicle.Register(
            VehicleTestData.NewPlate(),
            VehicleTestData.NewVin(),
            VehicleTestData.NewDescription(),
            VehicleTestData.NewOdometer(),
            owner,
            workshop,
            responsible,
            VehicleTestData.Now);

    private static Vehicle RegisteredVehicle() =>
        Register(VehicleTestData.Owner(), VehicleTestData.Workshop(), null).Value;

    private static void ClearEvents(Vehicle vehicle) => ((IAggregateRoot)vehicle).ClearDomainEvents();
}
