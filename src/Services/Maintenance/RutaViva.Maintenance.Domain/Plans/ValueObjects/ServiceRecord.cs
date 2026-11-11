using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record ServiceRecord : ValueObject
{
    private ServiceRecord(long kilometers, DateOnly date)
    {
        Kilometers = kilometers;
        Date = date;
    }

    public long Kilometers { get; }

    public DateOnly Date { get; }

    public static Result<ServiceRecord> Create(long kilometers, DateOnly date)
    {
        if (kilometers < 0)
        {
            return MaintenanceErrors.ServiceRecordKilometersNegative;
        }

        return new ServiceRecord(kilometers, date);
    }
}
