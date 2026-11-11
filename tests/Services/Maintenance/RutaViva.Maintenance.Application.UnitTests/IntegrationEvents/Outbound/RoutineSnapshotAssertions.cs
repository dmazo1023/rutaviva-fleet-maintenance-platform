using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

internal static class RoutineSnapshotAssertions
{
    public static void ShouldCopy(this RoutineSnapshot actual, RoutineStatusSnapshot expected)
    {
        AssertIdentity(actual, expected);
        AssertState(actual, expected);
        AssertCycle(actual, expected);
        AssertNextDue(actual, expected);
    }

    private static void AssertIdentity(RoutineSnapshot actual, RoutineStatusSnapshot expected)
    {
        actual.RoutineId.Should().Be(expected.RoutineId.Value);
        actual.VehicleId.Should().Be(expected.VehicleId.Value);
        actual.Plate.Should().Be(expected.Plate.Value);
        actual.CustomerId.Should().Be(expected.CustomerId.Value);
        actual.RoutineTypeCode.Should().Be(expected.RoutineTypeCode.Value);
        actual.RoutineName.Should().Be(expected.RoutineName);
    }

    private static void AssertState(RoutineSnapshot actual, RoutineStatusSnapshot expected)
    {
        actual.State.Should().Be(expected.State.ToString());
        actual.DueStatus.Should().Be(expected.DueStatus.ToString());
        actual.OdometerKm.Should().Be(expected.OdometerKm);
        actual.ConsumedPercent.Should().Be(expected.ConsumedPercent);
    }

    private static void AssertCycle(RoutineSnapshot actual, RoutineStatusSnapshot expected)
    {
        actual.CycleNumber.Should().Be(expected.CycleNumber);
        actual.CycleStartKm.Should().Be(expected.CycleStartKm);
        actual.CycleStartDate.Should().Be(expected.CycleStartDate);
        actual.IntervalKm.Should().Be(expected.IntervalKm);
        actual.IntervalMonths.Should().Be(expected.IntervalMonths);
        actual.EffectiveDueSoonPercent.Should().Be((int)expected.EffectiveDueSoonPercent);
    }

    private static void AssertNextDue(RoutineSnapshot actual, RoutineStatusSnapshot expected)
    {
        actual.NextDueKm.Should().Be(expected.NextDue.Kilometers);
        actual.NextDueDate.Should().Be(expected.NextDue.Date);
    }
}
