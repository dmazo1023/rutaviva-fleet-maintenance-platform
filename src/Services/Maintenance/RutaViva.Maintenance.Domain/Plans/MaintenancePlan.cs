using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.Evaluation;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Domain.Plans;

public sealed class MaintenancePlan : AggregateRoot<VehicleId>
{
    private readonly List<MaintenanceRoutine> _routines = [];

    private MaintenancePlan()
    {
        Plate = null!;
        Odometer = null!;
    }

    public LicensePlate Plate { get; private set; }

    public CustomerId CustomerId { get; private set; }

    public MaintenancePlanStatus Status { get; private set; }

    public Odometer Odometer { get; private set; }

    public DateOnly LastTimeEvaluationDate { get; private set; }

    public IReadOnlyCollection<MaintenanceRoutine> Routines => _routines.AsReadOnly();

    public bool CanBeEvaluated => Status == MaintenancePlanStatus.Active;

    public static MaintenancePlan Create(
        VehicleId id,
        LicensePlate plate,
        CustomerId customerId,
        MaintenancePlanStatus status,
        Odometer initial,
        DateOnly registeredOn) =>
        new()
        {
            Id = id,
            Plate = plate,
            CustomerId = customerId,
            Status = status,
            Odometer = initial,
            LastTimeEvaluationDate = registeredOn
        };

    public Result<RoutineId> RegisterRoutine(RoutineRegistration registration, RoutineParameterCatalog catalog, DateOnly today)
    {
        Result<RoutineTypeParameters> type = catalog.Find(registration.RoutineTypeId);
        if (type.IsFailure)
        {
            return type.Error;
        }

        if (HasRoutineOfType(registration.RoutineTypeId))
        {
            return MaintenanceErrors.RoutineAlreadyExists;
        }

        Result<MaintenanceCycle> cycle = StartCycle(registration, today);
        if (cycle.IsFailure)
        {
            return cycle.Error;
        }

        MaintenanceRoutine routine = MaintenanceRoutine.Create(registration.RoutineTypeId, type.Value, registration.Overrides, cycle.Value);
        _routines.Add(routine);
        CompleteRegistration(routine, type.Value, today);
        return routine.Id;
    }

    public OdometerReadingResult RecordOdometerReading(OdometerReading reading, RoutineParameterCatalog catalog)
    {
        if (!Odometer.IsAdvancedBy(reading))
        {
            return OdometerReadingResult.Ignored(OdometerReadingIgnoredReason.NotGreaterThanCurrent);
        }

        Odometer = Odometer.Advance(reading);
        if (!CanBeEvaluated)
        {
            return OdometerReadingResult.Accepted(ReevaluationOutcome.None);
        }

        EvaluationContext context = ContextAt(reading.Kilometers, LastTimeEvaluationDate);
        return OdometerReadingResult.Accepted(EvaluateActiveRoutines(context, catalog, reading.ReadAt));
    }

    public ReevaluationOutcome Reevaluate(DateOnly asOfDate, RoutineParameterCatalog catalog)
    {
        if (!CanBeEvaluated)
        {
            return ReevaluationOutcome.None;
        }

        DateOnly effectiveDate = LatestOf(asOfDate, LastTimeEvaluationDate);
        LastTimeEvaluationDate = effectiveDate;
        EvaluationContext context = ContextAt(Odometer.Kilometers, effectiveDate);
        return EvaluateActiveRoutines(context, catalog, StartOfDay(effectiveDate));
    }

    public Result UpdateRoutineOverrides(
        RoutineId id,
        RoutineOverrides overrides,
        string updatedBy,
        RoutineParameterCatalog catalog,
        DateOnly today)
    {
        Result<RoutineTarget> target = FindAuditedTarget(id, updatedBy, catalog);
        if (target.IsFailure)
        {
            return Result.Failure(target.Error);
        }

        Result<EffectiveRoutineParameters> effective = EffectiveRoutineParameters.Resolve(
            target.Value.Type.Interval,
            target.Value.Type.Threshold,
            overrides);
        if (effective.IsFailure)
        {
            return Result.Failure(effective.Error);
        }

        target.Value.Routine.ReplaceOverrides(overrides);
        ApplyAdministrativeChange(target.Value, today, ParametersChangedEvent);
        return Result.Success();
    }

    public Result ChangeRoutineState(
        RoutineId id,
        RoutineState state,
        string updatedBy,
        RoutineParameterCatalog catalog,
        DateOnly today)
    {
        Result<RoutineTarget> target = FindAuditedTarget(id, updatedBy, catalog);
        if (target.IsFailure)
        {
            return Result.Failure(target.Error);
        }

        if (target.Value.Routine.State == state)
        {
            return Result.Success();
        }

        target.Value.Routine.ChangeState(state);
        ApplyAdministrativeChange(target.Value, today, StateChangedEvent);
        return Result.Success();
    }

    public Result RestartRoutine(RoutineId id, ServiceRecord service, Guid serviceOrderId, RoutineParameterCatalog catalog)
    {
        Result<RoutineTarget> target = FindTarget(id, catalog);
        if (target.IsFailure)
        {
            return Result.Failure(target.Error);
        }

        if (target.Value.Routine.IsCycleStartedBy(service))
        {
            return Result.Success();
        }

        target.Value.Routine.Restart(service);
        AdvanceOdometerTo(service);
        DateOnly effectiveDate = LatestOf(service.Date, LastTimeEvaluationDate);
        RaiseChangeAndEvaluate(
            target.Value,
            effectiveDate,
            (snapshot, occurredAt) => new RoutineRestarted(snapshot, serviceOrderId, service, occurredAt));
        return Result.Success();
    }

    public void Suspend()
    {
        if (Status == MaintenancePlanStatus.Suspended)
        {
            return;
        }

        Status = MaintenancePlanStatus.Suspended;
        RaisePlanStatusChanged(LastTimeEvaluationDate);
    }

    public ReevaluationOutcome Resume(DateOnly today, RoutineParameterCatalog catalog)
    {
        if (Status == MaintenancePlanStatus.Active)
        {
            return ReevaluationOutcome.None;
        }

        Status = MaintenancePlanStatus.Active;
        RaisePlanStatusChanged(LatestOf(today, LastTimeEvaluationDate));
        return Reevaluate(today, catalog);
    }

    private static IDomainEvent ParametersChangedEvent(RoutineStatusSnapshot snapshot, DateTimeOffset occurredAt) =>
        new RoutineParametersChanged(snapshot, occurredAt);

    private static IDomainEvent StateChangedEvent(RoutineStatusSnapshot snapshot, DateTimeOffset occurredAt) =>
        new RoutineStateChanged(snapshot, occurredAt);

    private static DateOnly LatestOf(DateOnly first, DateOnly second)
    {
        if (first >= second)
        {
            return first;
        }

        return second;
    }

    private static DateTimeOffset StartOfDay(DateOnly date)
    {
        DateTime local = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, MaintenanceTimeZone.Bogota.GetUtcOffset(local));
    }

    private static EvaluationContext ContextAt(long odometerKm, DateOnly effectiveDate) =>
        EvaluationContext.Create(odometerKm, effectiveDate).Value;

    private static IDomainEvent EventFor(
        RoutineTransition transition,
        bool isFirstNoticeInCycle,
        RoutineStatusSnapshot snapshot,
        DateTimeOffset occurredAt) =>
        transition.Type switch
        {
            RoutineTransitionType.BecameDueSoon => new RoutineBecameDueSoon(snapshot, transition.Trigger, isFirstNoticeInCycle, occurredAt),
            RoutineTransitionType.BecameOverdue => new RoutineBecameOverdue(snapshot, transition.Trigger, occurredAt),
            _ => new RoutineDueStatusReverted(snapshot, occurredAt)
        };

    private Result<RoutineTarget> FindAuditedTarget(RoutineId id, string updatedBy, RoutineParameterCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
        {
            return MaintenanceErrors.UpdatedByRequired;
        }

        return FindTarget(id, catalog);
    }

    private Result<RoutineTarget> FindTarget(RoutineId id, RoutineParameterCatalog catalog)
    {
        MaintenanceRoutine? routine = _routines.FirstOrDefault(candidate => candidate.Id == id);
        if (routine is null)
        {
            return MaintenanceErrors.RoutineNotFound;
        }

        Result<RoutineTypeParameters> type = catalog.Find(routine.RoutineTypeId);
        if (type.IsFailure)
        {
            return type.Error;
        }

        return new RoutineTarget(routine, type.Value);
    }

    private void AdvanceOdometerTo(ServiceRecord service)
    {
        OdometerReading reading = OdometerReading.Create(service.Kilometers, StartOfDay(service.Date)).Value;
        Odometer = Odometer.Advance(reading);
    }

    private void RaisePlanStatusChanged(DateOnly effectiveDate) =>
        Raise(new MaintenancePlanStatusChanged(Id, Plate, CustomerId, Status, StartOfDay(effectiveDate)));

    private void ApplyAdministrativeChange(
        RoutineTarget target,
        DateOnly today,
        Func<RoutineStatusSnapshot, DateTimeOffset, IDomainEvent> changeEvent)
    {
        DateOnly effectiveDate = LatestOf(today, LastTimeEvaluationDate);
        RaiseChangeAndEvaluate(target, effectiveDate, changeEvent);
        if (CanBeEvaluated && target.Routine.IsActive)
        {
            LastTimeEvaluationDate = effectiveDate;
        }
    }

    private void RaiseChangeAndEvaluate(
        RoutineTarget target,
        DateOnly effectiveDate,
        Func<RoutineStatusSnapshot, DateTimeOffset, IDomainEvent> changeEvent)
    {
        EvaluationContext context = ContextAt(Odometer.Kilometers, effectiveDate);
        DateTimeOffset occurredAt = StartOfDay(effectiveDate);
        EffectiveRoutineParameters parameters = target.Routine.ResolveParameters(target.Type);
        RoutineAssessment assessment = RoutineAssessor.Assess(target.Routine.Cycle, parameters, context);

        Raise(changeEvent(BuildSnapshot(target.Routine, parameters, assessment, context.OdometerKm), occurredAt));
        if (!CanBeEvaluated || !target.Routine.IsActive)
        {
            return;
        }

        ApplyAssessment(target.Routine, parameters, assessment, context.OdometerKm, occurredAt);
    }

    private bool HasRoutineOfType(RoutineTypeId routineTypeId) =>
        _routines.Any(routine => routine.RoutineTypeId == routineTypeId);

    private Result<MaintenanceCycle> StartCycle(RoutineRegistration registration, DateOnly today)
    {
        if (registration.HasNoLastService)
        {
            return MaintenanceCycle.Start(Odometer.Kilometers, today);
        }

        Result<ServiceRecord> service = registration.ToServiceRecord();
        if (service.IsFailure)
        {
            return service.Error;
        }

        if (service.Value.Kilometers > Odometer.Kilometers)
        {
            return MaintenanceErrors.RoutineLastServiceKmAboveOdometer;
        }

        return MaintenanceCycle.Start(service.Value.Kilometers, service.Value.Date);
    }

    private void CompleteRegistration(MaintenanceRoutine routine, RoutineTypeParameters type, DateOnly today)
    {
        DateOnly effectiveDate = LatestOf(today, LastTimeEvaluationDate);
        EvaluationContext context = ContextAt(Odometer.Kilometers, effectiveDate);
        DateTimeOffset occurredAt = StartOfDay(effectiveDate);
        EffectiveRoutineParameters parameters = routine.ResolveParameters(type);
        RoutineAssessment assessment = RoutineAssessor.Assess(routine.Cycle, parameters, context);

        Raise(new RoutineRegistered(BuildSnapshot(routine, parameters, assessment, context.OdometerKm), occurredAt));
        if (!CanBeEvaluated)
        {
            return;
        }

        LastTimeEvaluationDate = effectiveDate;
        ApplyAssessment(routine, parameters, assessment, context.OdometerKm, occurredAt);
    }

    private ReevaluationOutcome EvaluateActiveRoutines(EvaluationContext context, RoutineParameterCatalog catalog, DateTimeOffset occurredAt)
    {
        ReevaluationOutcome outcome = ReevaluationOutcome.None;
        foreach (MaintenanceRoutine routine in _routines.Where(candidate => candidate.IsActive))
        {
            outcome = outcome.Count(EvaluateRoutine(routine, catalog, context, occurredAt));
        }

        return outcome;
    }

    private RoutineTransitionType EvaluateRoutine(
        MaintenanceRoutine routine,
        RoutineParameterCatalog catalog,
        EvaluationContext context,
        DateTimeOffset occurredAt)
    {
        Result<RoutineTypeParameters> type = catalog.Find(routine.RoutineTypeId);
        if (type.IsFailure)
        {
            return RoutineTransitionType.None;
        }

        EffectiveRoutineParameters parameters = routine.ResolveParameters(type.Value);
        RoutineAssessment assessment = RoutineAssessor.Assess(routine.Cycle, parameters, context);
        return ApplyAssessment(routine, parameters, assessment, context.OdometerKm, occurredAt);
    }

    private RoutineTransitionType ApplyAssessment(
        MaintenanceRoutine routine,
        EffectiveRoutineParameters parameters,
        RoutineAssessment assessment,
        long odometerKm,
        DateTimeOffset occurredAt)
    {
        bool isFirstNoticeInCycle = !routine.DueSoonNotified;
        RoutineTransition transition = routine.ApplyAssessment(assessment);
        if (!transition.HasChanged)
        {
            return transition.Type;
        }

        RoutineStatusSnapshot snapshot = BuildSnapshot(routine, parameters, assessment, odometerKm);
        Raise(EventFor(transition, isFirstNoticeInCycle, snapshot, occurredAt));
        return transition.Type;
    }

    private RoutineStatusSnapshot BuildSnapshot(
        MaintenanceRoutine routine,
        EffectiveRoutineParameters parameters,
        RoutineAssessment assessment,
        long odometerKm) =>
        new(
            routine.Id,
            Id,
            Plate,
            CustomerId,
            routine.RoutineTypeCode,
            routine.Name,
            routine.State,
            routine.DueStatus,
            routine.Cycle.Number,
            routine.Cycle.StartKm,
            routine.Cycle.StartDate,
            parameters.Interval.Kilometers,
            parameters.Interval.Months,
            parameters.DueSoonThreshold.Percent,
            NextDue.Calculate(routine.Cycle, parameters.Interval),
            odometerKm,
            assessment.ConsumedPercent);

    private readonly record struct RoutineTarget(MaintenanceRoutine Routine, RoutineTypeParameters Type);
}
