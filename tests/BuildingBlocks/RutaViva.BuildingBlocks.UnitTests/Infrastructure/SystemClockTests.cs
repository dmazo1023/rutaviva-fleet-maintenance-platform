using RutaViva.BuildingBlocks.Infrastructure.Time;

namespace RutaViva.BuildingBlocks.UnitTests.Infrastructure;

public sealed class SystemClockTests
{
    private const string BogotaZoneId = "America/Bogota";

    [Fact]
    public void TodayIn_Should_ReturnPreviousDay_When_UtcIsJustAfterMidnightAndZoneIsBehind()
    {
        SystemClock clock = new(new FixedTimeProvider(new DateTimeOffset(2026, 3, 10, 2, 30, 0, TimeSpan.Zero)));
        TimeZoneInfo bogota = TimeZoneInfo.FindSystemTimeZoneById(BogotaZoneId);

        DateOnly today = clock.TodayIn(bogota);

        today.Should().Be(new DateOnly(2026, 3, 9));
    }

    [Fact]
    public void TodayIn_Should_ReturnSameDay_When_UtcIsAfterZoneMidnight()
    {
        SystemClock clock = new(new FixedTimeProvider(new DateTimeOffset(2026, 3, 10, 5, 0, 0, TimeSpan.Zero)));
        TimeZoneInfo bogota = TimeZoneInfo.FindSystemTimeZoneById(BogotaZoneId);

        DateOnly today = clock.TodayIn(bogota);

        today.Should().Be(new DateOnly(2026, 3, 10));
    }

    [Fact]
    public void UtcNow_Should_ReturnProviderInstant_When_Read()
    {
        DateTimeOffset instant = new(2026, 3, 10, 2, 30, 0, TimeSpan.Zero);
        SystemClock clock = new(new FixedTimeProvider(instant));

        clock.UtcNow.Should().Be(instant);
    }

    private sealed class FixedTimeProvider(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
