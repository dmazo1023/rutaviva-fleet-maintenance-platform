using RutaViva.Maintenance.Application.Abstractions;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Repositories;

public sealed class OdometerReadingLog(MaintenanceDbContext context) : IOdometerReadingLog
{
    public void Append(OdometerReadingLogEntry entry) => context.OdometerReadings.Add(ToRecord(entry));

    private static OdometerReadingRecord ToRecord(OdometerReadingLogEntry entry) => new()
    {
        VehicleId = entry.VehicleId,
        Kilometers = entry.Kilometers,
        ReadAt = entry.ReadAt,
        ReceivedAt = entry.ReceivedAt,
        Outcome = entry.Outcome,
        IgnoredReason = entry.IgnoredReason
    };
}
