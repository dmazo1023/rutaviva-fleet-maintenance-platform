using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

public sealed class RestartRoutineIdempotencyTests
{
    private const long ServiceKm = 98400;
    private const long NextServiceKm = 98900;
    private const int ExpectedSecondCycle = 2;
    private const int ExpectedThirdCycle = 3;
    private const int DeliveryCount = 5;

    private static readonly DateOnly ServiceDate = new(2026, 11, 12);
    private static readonly DateOnly NextServiceDate = new(2026, 11, 13);

    [Fact]
    public void RestartRoutine_Should_ReturnSuccessWithoutChanges_When_SameServiceIsDeliveredAgain()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(plan, SampleCatalog.BrakesCode);
        Guid serviceOrderId = Guid.NewGuid();
        plan.RestartRoutine(brakes.Id, Service(ServiceKm, ServiceDate), serviceOrderId, SampleCatalog.Catalog);
        SampleFleet.ClearEvents([plan]);

        Result repeated = plan.RestartRoutine(brakes.Id, Service(ServiceKm, ServiceDate), serviceOrderId, SampleCatalog.Catalog);

        repeated.IsSuccess.Should().BeTrue();
        brakes.Cycle.Number.Should().Be(ExpectedSecondCycle);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RestartRoutine_Should_EmitSingleRestartedEvent_When_SameServiceIsDeliveredFiveTimes()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(plan, SampleCatalog.BrakesCode);
        SampleFleet.ClearEvents([plan]);
        Guid serviceOrderId = Guid.NewGuid();

        for (int delivery = 0; delivery < DeliveryCount; delivery++)
        {
            plan.RestartRoutine(brakes.Id, Service(ServiceKm, ServiceDate), serviceOrderId, SampleCatalog.Catalog);
        }

        plan.DomainEvents.OfType<RoutineRestarted>().Should().ContainSingle();
        brakes.Cycle.Number.Should().Be(ExpectedSecondCycle);
    }

    [Fact]
    public void RestartRoutine_Should_StartNewCycle_When_ServiceDiffersFromCurrentCycleStart()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(plan, SampleCatalog.BrakesCode);
        plan.RestartRoutine(brakes.Id, Service(ServiceKm, ServiceDate), Guid.NewGuid(), SampleCatalog.Catalog);

        Result next = plan.RestartRoutine(brakes.Id, Service(NextServiceKm, NextServiceDate), Guid.NewGuid(), SampleCatalog.Catalog);

        next.IsSuccess.Should().BeTrue();
        brakes.Cycle.Number.Should().Be(ExpectedThirdCycle);
        brakes.Cycle.StartKm.Should().Be(NextServiceKm);
    }

    private static ServiceRecord Service(long kilometers, DateOnly date) => ServiceRecord.Create(kilometers, date).Value;
}
