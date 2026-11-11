using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record DueSoonThreshold : ValueObject
{
    public const decimal MinExclusivePercent = 0m;
    public const decimal MaxExclusivePercent = 100m;

    private DueSoonThreshold(decimal percent) => Percent = percent;

    public decimal Percent { get; }

    public static Result<DueSoonThreshold> Create(decimal percent)
    {
        if (!IsValid(percent))
        {
            return MaintenanceErrors.DueSoonThresholdOutOfRange;
        }

        return new DueSoonThreshold(percent);
    }

    public decimal Of(decimal amount) => amount * Percent / MaxExclusivePercent;

    internal static bool IsValid(decimal percent) => percent > MinExclusivePercent && percent < MaxExclusivePercent;
}
