using System.Collections.Frozen;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Alerts.Events;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Alerts;

public sealed class Alert : AggregateRoot<AlertId>
{
    private static readonly FrozenSet<(AlertStatus From, AlertStatus To)> AllowedTransitions = new HashSet<(AlertStatus From, AlertStatus To)>
    {
        (AlertStatus.Active, AlertStatus.Acknowledged),
        (AlertStatus.Active, AlertStatus.Resolved),
        (AlertStatus.Acknowledged, AlertStatus.Resolved)
    }.ToFrozenSet();

    private Alert()
    {
    }

    public VehicleId VehicleId { get; private set; }

    public LicensePlate Plate { get; private set; } = null!;

    public CustomerId CustomerId { get; private set; }

    public RoutineId RoutineId { get; private set; }

    public string RoutineName { get; private set; } = string.Empty;

    public int CycleNumber { get; private set; }

    public AlertType Type { get; private set; }

    public AlertTrigger Trigger { get; private set; }

    public AlertStatus Status { get; private set; }

    public DateTimeOffset RaisedAt { get; private set; }

    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public string? AcknowledgedBy { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public AlertResolutionReason? ResolutionReason { get; private set; }

    public static Alert Raise(AlertOrigin origin, AlertType type, AlertTrigger trigger, DateTimeOffset raisedAt)
    {
        Alert alert = new()
        {
            Id = AlertId.New(),
            VehicleId = origin.VehicleId,
            Plate = origin.Plate,
            CustomerId = origin.CustomerId,
            RoutineId = origin.RoutineId,
            RoutineName = origin.RoutineName,
            CycleNumber = origin.CycleNumber,
            Type = type,
            Trigger = trigger,
            Status = AlertStatus.Active,
            RaisedAt = raisedAt
        };

        alert.Raise(alert.ToRaisedEvent());
        return alert;
    }

    public Result Acknowledge(string acknowledgedBy, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(acknowledgedBy))
        {
            return Result.Failure(MaintenanceErrors.AlertAcknowledgedByRequired);
        }

        Result transition = EnsureCanMoveTo(AlertStatus.Acknowledged);
        if (transition.IsFailure)
        {
            return transition;
        }

        Status = AlertStatus.Acknowledged;
        AcknowledgedAt = at;
        AcknowledgedBy = acknowledgedBy;
        Raise(ToStatusChangedEvent(at));
        return Result.Success();
    }

    public Result Resolve(AlertResolutionReason reason, DateTimeOffset at)
    {
        if (Status == AlertStatus.Resolved)
        {
            return Result.Success();
        }

        Result transition = EnsureCanMoveTo(AlertStatus.Resolved);
        if (transition.IsFailure)
        {
            return transition;
        }

        Status = AlertStatus.Resolved;
        ResolvedAt = at;
        ResolutionReason = reason;
        Raise(ToStatusChangedEvent(at));
        return Result.Success();
    }

    private Result EnsureCanMoveTo(AlertStatus target)
    {
        if (!AllowedTransitions.Contains((Status, target)))
        {
            return Result.Failure(MaintenanceErrors.AlertInvalidTransition);
        }

        return Result.Success();
    }

    private AlertRaised ToRaisedEvent() => new(
        Id,
        VehicleId,
        Plate,
        CustomerId,
        RoutineId,
        RoutineName,
        CycleNumber,
        Type,
        Trigger,
        Status,
        RaisedAt,
        RaisedAt);

    private AlertStatusChanged ToStatusChangedEvent(DateTimeOffset occurredAt) => new(
        Id,
        VehicleId,
        Plate,
        CustomerId,
        RoutineId,
        Type,
        Trigger,
        Status,
        AcknowledgedAt,
        AcknowledgedBy,
        ResolvedAt,
        ResolutionReason,
        occurredAt);
}
