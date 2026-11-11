using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class NextDueTests
{
    private const long CycleStartKm = 45000;
    private const int IntervalKm = 10000;
    private const int IntervalMonths = 6;
    private const long ExpectedKm = 55000;
    private const long ServiceKm = 98400;
    private const int BrakesIntervalKm = 20000;
    private const int BrakesIntervalMonths = 12;
    private const long ExpectedBrakesKm = 118400;

    private static readonly DateOnly CycleStartDate = new(2026, 8, 1);
    private static readonly DateOnly ExpectedDate = new(2027, 2, 1);
    private static readonly DateOnly ServiceDate = new(2026, 11, 12);
    private static readonly DateOnly ExpectedBrakesDate = new(2027, 11, 12);

    [Fact]
    public void Calculate_Should_ReturnKilometersAndDate_When_IntervalHasBothDimensions()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(CycleStartKm, CycleStartDate).Value;
        MaintenanceInterval interval = MaintenanceInterval.Create(IntervalKm, IntervalMonths).Value;

        NextDue nextDue = NextDue.Calculate(cycle, interval);

        nextDue.Kilometers.Should().Be(ExpectedKm);
        nextDue.Date.Should().Be(ExpectedDate);
    }

    [Fact]
    public void Calculate_Should_ReturnOnlyKilometers_When_IntervalHasNoMonths()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(CycleStartKm, CycleStartDate).Value;
        MaintenanceInterval interval = MaintenanceInterval.Create(IntervalKm, null).Value;

        NextDue nextDue = NextDue.Calculate(cycle, interval);

        nextDue.Kilometers.Should().Be(ExpectedKm);
        nextDue.Date.Should().BeNull();
    }

    [Fact]
    public void Calculate_Should_ReturnOnlyDate_When_IntervalHasNoKilometers()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(CycleStartKm, CycleStartDate).Value;
        MaintenanceInterval interval = MaintenanceInterval.Create(null, IntervalMonths).Value;

        NextDue nextDue = NextDue.Calculate(cycle, interval);

        nextDue.Kilometers.Should().BeNull();
        nextDue.Date.Should().Be(ExpectedDate);
    }

    [Fact]
    public void Calculate_Should_UseServiceAsCycleStart_When_CycleIsRestarted()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(CycleStartKm, CycleStartDate).Value;
        ServiceRecord service = ServiceRecord.Create(ServiceKm, ServiceDate).Value;
        MaintenanceInterval interval = MaintenanceInterval.Create(BrakesIntervalKm, BrakesIntervalMonths).Value;

        NextDue nextDue = NextDue.Calculate(cycle.Next(service), interval);

        nextDue.Kilometers.Should().Be(ExpectedBrakesKm);
        nextDue.Date.Should().Be(ExpectedBrakesDate);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_CalculatedFromSameInputs()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(CycleStartKm, CycleStartDate).Value;
        MaintenanceInterval interval = MaintenanceInterval.Create(IntervalKm, IntervalMonths).Value;

        NextDue.Calculate(cycle, interval).Should().Be(NextDue.Calculate(cycle, interval));
    }
}
