namespace RutaViva.Maintenance.Infrastructure.Scheduling;

public static class NextRunCalculator
{
    private const int NextDayOffset = 1;
    private const int DaylightGapShiftHours = 1;

    public static DateTimeOffset Calculate(DateTimeOffset utcNow, TimeOnly runAt, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        DateTime localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone).DateTime;
        DateTime todayRun = DateOnly.FromDateTime(localNow).ToDateTime(runAt);
        DateTime nextLocalRun = todayRun > localNow ? todayRun : todayRun.AddDays(NextDayOffset);
        return ToUtc(nextLocalRun, timeZone);
    }

    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo timeZone)
    {
        DateTime unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        DateTime valid = timeZone.IsInvalidTime(unspecified) ? unspecified.AddHours(DaylightGapShiftHours) : unspecified;
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(valid, timeZone), TimeSpan.Zero);
    }
}
