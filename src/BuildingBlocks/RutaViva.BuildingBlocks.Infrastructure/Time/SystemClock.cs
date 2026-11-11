using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.Infrastructure.Time;

public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly TodayIn(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        DateTimeOffset local = TimeZoneInfo.ConvertTime(UtcNow, timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
