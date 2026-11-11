namespace RutaViva.Maintenance.Domain.Common;

public static class MaintenanceTimeZone
{
    public const string BogotaId = "America/Bogota";

    public static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById(BogotaId);

    public static DateOnly ToLocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Bogota).DateTime);
}
