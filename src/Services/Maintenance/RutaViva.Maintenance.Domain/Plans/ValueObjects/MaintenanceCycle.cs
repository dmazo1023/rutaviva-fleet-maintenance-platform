using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record MaintenanceCycle : ValueObject
{
    public const int FirstNumber = 1;

    private MaintenanceCycle(int number, long startKm, DateOnly startDate)
    {
        Number = number;
        StartKm = startKm;
        StartDate = startDate;
    }

    public int Number { get; }

    public long StartKm { get; }

    public DateOnly StartDate { get; }

    public static Result<MaintenanceCycle> Create(int number, long startKm, DateOnly startDate)
    {
        if (number < FirstNumber)
        {
            return MaintenanceErrors.MaintenanceCycleNumberInvalid;
        }

        if (startKm < 0)
        {
            return MaintenanceErrors.MaintenanceCycleStartKmNegative;
        }

        return new MaintenanceCycle(number, startKm, startDate);
    }

    public static Result<MaintenanceCycle> Start(long startKm, DateOnly startDate) => Create(FirstNumber, startKm, startDate);

    public MaintenanceCycle Next(ServiceRecord service) => new(Number + 1, service.Kilometers, service.Date);
}
