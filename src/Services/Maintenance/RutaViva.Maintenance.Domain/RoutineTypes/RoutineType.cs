using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes.Events;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.RoutineTypes;

public sealed class RoutineType : AggregateRoot<RoutineTypeId>
{
    private RoutineType()
    {
        Code = null!;
        Name = null!;
        DefaultInterval = null!;
        DueSoonThreshold = null!;
        UpdatedBy = null!;
    }

    public RoutineTypeCode Code { get; private set; }

    public string Name { get; private set; }

    public MaintenanceInterval DefaultInterval { get; private set; }

    public DueSoonThreshold DueSoonThreshold { get; private set; }

    public RoutineTypeStatus Status { get; private set; }

    public string UpdatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<RoutineType> Create(
        RoutineTypeId id,
        RoutineTypeCode code,
        string name,
        MaintenanceInterval defaultInterval,
        DueSoonThreshold dueSoonThreshold,
        string updatedBy,
        DateTimeOffset at)
    {
        Error? invalid = ValidateName(name) ?? ValidateUpdatedBy(updatedBy);
        if (invalid is not null)
        {
            return invalid;
        }

        return new RoutineType
        {
            Id = id,
            Code = code,
            Name = name.Trim(),
            DefaultInterval = defaultInterval,
            DueSoonThreshold = dueSoonThreshold,
            Status = RoutineTypeStatus.Active,
            UpdatedBy = updatedBy.Trim(),
            UpdatedAt = at
        };
    }

    public Result UpdateParameters(
        MaintenanceInterval interval,
        DueSoonThreshold threshold,
        string updatedBy,
        DateTimeOffset at)
    {
        Error? invalid = ValidateUpdatedBy(updatedBy);
        if (invalid is not null)
        {
            return Result.Failure(invalid);
        }

        if (!ChangesParameters(interval, threshold))
        {
            return Result.Success();
        }

        Apply(interval, threshold, updatedBy.Trim(), at);
        Raise(new RoutineTypeParametersChanged(Id, at));
        return Result.Success();
    }

    public RoutineTypeParameters ToParameters() => new(Code, Name, DefaultInterval, DueSoonThreshold);

    private static Error? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return MaintenanceErrors.RoutineTypeNameRequired;
        }

        return null;
    }

    private static Error? ValidateUpdatedBy(string? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
        {
            return MaintenanceErrors.UpdatedByRequired;
        }

        return null;
    }

    private bool ChangesParameters(MaintenanceInterval interval, DueSoonThreshold threshold) =>
        DefaultInterval != interval || DueSoonThreshold != threshold;

    private void Apply(MaintenanceInterval interval, DueSoonThreshold threshold, string updatedBy, DateTimeOffset at)
    {
        DefaultInterval = interval;
        DueSoonThreshold = threshold;
        UpdatedBy = updatedBy;
        UpdatedAt = at;
    }
}
