using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.Evaluation;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans;

public sealed class MaintenanceRoutine : Entity<RoutineId>
{
    private MaintenanceRoutine()
    {
        RoutineTypeCode = null!;
        Name = null!;
        Overrides = null!;
        Cycle = null!;
    }

    public RoutineTypeId RoutineTypeId { get; private set; }

    public RoutineTypeCode RoutineTypeCode { get; private set; }

    public string Name { get; private set; }

    public RoutineOverrides Overrides { get; private set; }

    public RoutineState State { get; private set; }

    public RoutineDueStatus DueStatus { get; private set; }

    public MaintenanceCycle Cycle { get; private set; }

    public bool DueSoonNotified { get; private set; }

    public AlertTrigger? LastTrigger { get; private set; }

    public bool IsActive => State == RoutineState.Active;

    internal static MaintenanceRoutine Create(
        RoutineTypeId routineTypeId,
        RoutineTypeParameters type,
        RoutineOverrides overrides,
        MaintenanceCycle cycle) =>
        new()
        {
            Id = RoutineId.New(),
            RoutineTypeId = routineTypeId,
            RoutineTypeCode = type.Code,
            Name = type.Name,
            Overrides = overrides,
            State = RoutineState.Active,
            DueStatus = RoutineDueStatus.UpToDate,
            Cycle = cycle,
            DueSoonNotified = false
        };

    internal RoutineTransition ApplyAssessment(RoutineAssessment assessment)
    {
        RoutineTransition transition = RoutineTransitionPolicy.Decide(DueStatus, assessment);
        if (!transition.HasChanged)
        {
            return transition;
        }

        DueStatus = transition.Status;
        LastTrigger = TriggerAfter(transition);
        MarkDueSoonNotified(transition);
        return transition;
    }

    internal void ReplaceOverrides(RoutineOverrides overrides) => Overrides = overrides;

    internal void ChangeState(RoutineState state) => State = state;

    internal bool IsCycleStartedBy(ServiceRecord service) =>
        Cycle.StartKm == service.Kilometers && Cycle.StartDate == service.Date;

    internal void Restart(ServiceRecord service)
    {
        Cycle = Cycle.Next(service);
        DueStatus = RoutineDueStatus.UpToDate;
        DueSoonNotified = false;
        LastTrigger = null;
    }

    internal EffectiveRoutineParameters ResolveParameters(RoutineTypeParameters type) =>
        EffectiveRoutineParameters.Resolve(type.Interval, type.Threshold, Overrides).Value;

    private static AlertTrigger? TriggerAfter(RoutineTransition transition)
    {
        if (transition.Type == RoutineTransitionType.RevertedToUpToDate)
        {
            return null;
        }

        return transition.Trigger;
    }

    private void MarkDueSoonNotified(RoutineTransition transition)
    {
        if (transition.Type == RoutineTransitionType.BecameDueSoon)
        {
            DueSoonNotified = true;
        }
    }
}
