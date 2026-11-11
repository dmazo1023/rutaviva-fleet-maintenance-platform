using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.Testing.Fixtures;

public sealed class FakeClock(DateTimeOffset initialUtcNow) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = initialUtcNow.ToUniversalTime();

    public DateOnly TodayIn(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        DateTimeOffset local = TimeZoneInfo.ConvertTime(UtcNow, timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    public void Set(DateTimeOffset utcNow) => UtcNow = utcNow.ToUniversalTime();

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
