using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record EvaluationContext : ValueObject
{
    private EvaluationContext(long odometerKm, DateOnly effectiveDate)
    {
        OdometerKm = odometerKm;
        EffectiveDate = effectiveDate;
    }

    public long OdometerKm { get; }

    public DateOnly EffectiveDate { get; }

    public static Result<EvaluationContext> Create(long odometerKm, DateOnly effectiveDate)
    {
        if (odometerKm < 0)
        {
            return MaintenanceErrors.EvaluationContextKilometersNegative;
        }

        return new EvaluationContext(odometerKm, effectiveDate);
    }
}
