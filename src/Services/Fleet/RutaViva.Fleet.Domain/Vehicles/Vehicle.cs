using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.Vehicles;

public sealed class Vehicle : AggregateRoot<VehicleId>
{
    private Vehicle()
    {
        Plate = null!;
        Description = null!;
        InitialOdometer = null!;
    }

    public Plate Plate { get; private set; }

    public Vin? Vin { get; private set; }

    public VehicleDescription Description { get; private set; }

    public Kilometers InitialOdometer { get; private set; }

    public CustomerId CustomerId { get; private set; }

    public WorkshopId WorkshopId { get; private set; }

    public ApproverId? ResponsibleApproverId { get; private set; }

    public VehicleStatus Status { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset StatusChangedAt { get; private set; }

    public static Result<Vehicle> Register(
        Plate plate,
        Vin? vin,
        VehicleDescription description,
        Kilometers initialOdometer,
        CustomerReference owner,
        WorkshopReference workshop,
        ApproverReference? responsible,
        DateTimeOffset now)
    {
        Error? invalid = ValidateOwner(owner) ?? ValidateWorkshop(workshop) ?? ValidateResponsible(responsible);
        if (invalid is not null)
        {
            return invalid;
        }

        Vehicle vehicle = new()
        {
            Id = VehicleId.New(),
            Plate = plate,
            Vin = vin,
            Description = description,
            InitialOdometer = initialOdometer,
            CustomerId = owner.Id,
            WorkshopId = workshop.Id,
            ResponsibleApproverId = responsible?.Id,
            Status = VehicleStatus.Active,
            RegisteredAt = now,
            StatusChangedAt = now
        };
        vehicle.Raise(new VehicleRegistered(
            vehicle.Id,
            plate,
            vin,
            description,
            vehicle.CustomerId,
            vehicle.WorkshopId,
            vehicle.ResponsibleApproverId,
            vehicle.Status,
            initialOdometer,
            now,
            now));
        return vehicle;
    }

    public Result AssignResponsible(ApproverReference? responsible) =>
        AssignResponsible(responsible, TimeProvider.System.GetUtcNow());

    public Result AssignResponsible(ApproverReference? responsible, DateTimeOffset now)
    {
        ApproverId? responsibleId = responsible?.Id;
        if (ResponsibleApproverId == responsibleId)
        {
            return Result.Success();
        }

        Error? invalid = ValidateResponsible(responsible);
        if (invalid is not null)
        {
            return Result.Failure(invalid);
        }

        ResponsibleApproverId = responsibleId;
        Raise(new VehicleResponsibleAssigned(Id, responsibleId, now));
        return Result.Success();
    }

    public Result Deactivate(DateTimeOffset now) => ChangeStatus(VehicleStatus.Inactive, now);

    public Result Reactivate(DateTimeOffset now) => ChangeStatus(VehicleStatus.Active, now);

    private static Error? ValidateOwner(CustomerReference owner)
    {
        if (!owner.IsActive)
        {
            return VehicleErrors.OwnerInactive;
        }

        return null;
    }

    private static Error? ValidateWorkshop(WorkshopReference workshop)
    {
        if (!workshop.IsActive)
        {
            return VehicleErrors.WorkshopInactive;
        }

        return null;
    }

    private static Error? ValidateResponsible(ApproverReference? responsible)
    {
        if (responsible is not null && !responsible.IsActive)
        {
            return VehicleErrors.ResponsibleInactive;
        }

        return null;
    }

    private Result ChangeStatus(VehicleStatus target, DateTimeOffset now)
    {
        if (Status == target)
        {
            return Result.Success();
        }

        Status = target;
        StatusChangedAt = now;
        Raise(new VehicleStatusChanged(Id, Plate, CustomerId, target, now));
        return Result.Success();
    }
}
