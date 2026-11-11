using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Infrastructure.Scheduling;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Scheduling;

public sealed class NextRunCalculatorTests
{
    private const string BeforeMidnightUtc = "2026-03-11T04:59:00+00:00";
    private const string AfterRunUtc = "2026-03-11T05:06:00+00:00";
    private const string BeforeRunUtc = "2026-03-11T05:04:00+00:00";
    private const string AtRunUtc = "2026-03-11T05:05:00+00:00";
    private const string RunOnMarchElevenUtc = "2026-03-11T05:05:00+00:00";
    private const string RunOnMarchTwelveUtc = "2026-03-12T05:05:00+00:00";

    private const int ExpectedHour = 0;
    private const int ExpectedMinute = 5;

    private static readonly TimeOnly RunAt = TimeEvaluationScheduleOptions.DefaultRunAt;

    [Fact]
    public void Calculate_Should_ReturnNextDayRun_When_LocalTimeIs2359()
    {
        IClock clock = new FakeClock(DateTimeOffset.Parse(BeforeMidnightUtc));

        DateTimeOffset next = NextRunCalculator.Calculate(clock.UtcNow, RunAt, MaintenanceTimeZone.Bogota);

        next.Should().Be(DateTimeOffset.Parse(RunOnMarchElevenUtc));
    }

    [Fact]
    public void Calculate_Should_ReturnNextDayRun_When_LocalTimeIs0006()
    {
        IClock clock = new FakeClock(DateTimeOffset.Parse(AfterRunUtc));

        DateTimeOffset next = NextRunCalculator.Calculate(clock.UtcNow, RunAt, MaintenanceTimeZone.Bogota);

        next.Should().Be(DateTimeOffset.Parse(RunOnMarchTwelveUtc));
    }

    [Fact]
    public void Calculate_Should_ReturnSameDayRun_When_LocalTimeIs0004()
    {
        IClock clock = new FakeClock(DateTimeOffset.Parse(BeforeRunUtc));

        DateTimeOffset next = NextRunCalculator.Calculate(clock.UtcNow, RunAt, MaintenanceTimeZone.Bogota);

        next.Should().Be(DateTimeOffset.Parse(RunOnMarchElevenUtc));
    }

    [Fact]
    public void Calculate_Should_ReturnNextDayRun_When_LocalTimeIsExactlyRunAt()
    {
        IClock clock = new FakeClock(DateTimeOffset.Parse(AtRunUtc));

        DateTimeOffset next = NextRunCalculator.Calculate(clock.UtcNow, RunAt, MaintenanceTimeZone.Bogota);

        next.Should().Be(DateTimeOffset.Parse(RunOnMarchTwelveUtc));
    }

    [Fact]
    public void Defaults_Should_BeBogotaAtFiveMinutesPastMidnight_When_NothingConfigured()
    {
        TimeEvaluationScheduleOptions options = new();

        options.RunAt.Should().Be(new TimeOnly(ExpectedHour, ExpectedMinute));
        options.TimeZoneId.Should().Be(MaintenanceTimeZone.BogotaId);
    }
}
