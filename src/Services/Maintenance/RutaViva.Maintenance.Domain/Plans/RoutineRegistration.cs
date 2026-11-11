using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans;

public sealed record RoutineRegistration
{
    private RoutineRegistration(
        RoutineTypeId routineTypeId,
        long? lastServiceKm,
        DateOnly? lastServiceDate,
        RoutineOverrides overrides)
    {
        RoutineTypeId = routineTypeId;
        LastServiceKm = lastServiceKm;
        LastServiceDate = lastServiceDate;
        Overrides = overrides;
    }

    public RoutineTypeId RoutineTypeId { get; }

    public long? LastServiceKm { get; }

    public DateOnly? LastServiceDate { get; }

    public RoutineOverrides Overrides { get; }

    public bool HasNoLastService => LastServiceKm is null && LastServiceDate is null;

    public static RoutineRegistration Create(
        RoutineTypeId routineTypeId,
        ServiceRecord? lastService,
        RoutineOverrides overrides) =>
        new(routineTypeId, lastService?.Kilometers, lastService?.Date, overrides);

    public static RoutineRegistration Create(
        RoutineTypeId routineTypeId,
        long? lastServiceKm,
        DateOnly? lastServiceDate,
        RoutineOverrides overrides) =>
        new(routineTypeId, lastServiceKm, lastServiceDate, overrides);

    internal Result<ServiceRecord> ToServiceRecord()
    {
        if (LastServiceKm is not { } kilometers || LastServiceDate is not { } date)
        {
            return MaintenanceErrors.RoutineIncompleteLastService;
        }

        return ServiceRecord.Create(kilometers, date);
    }
}
