using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.UnitTests.Plans;

public sealed class RestartRoutineHandlerTests
{
    private const long ServicedKm = 50500;
    private const int SecondCycle = 2;

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RestartRoutineHandler _handler;

    public RestartRoutineHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(PlanTestFactory.Catalog()));
        _handler = new RestartRoutineHandler(_plans, _routineTypes, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_RestartCycleWithLocalServiceDateAndSave_When_RoutineExists()
    {
        MaintenancePlan plan = PlanTestFactory.PlanWithRoutine(VehicleId.New(), out RoutineId routineId);
        _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(plan));
        RestartRoutineCommand command = new(plan.Id, routineId, ServicedKm, PlanTestFactory.LateUtcInstant(), Guid.NewGuid());

        Result<RoutineId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        MaintenanceRoutine routine = plan.Routines.Single();
        routine.Cycle.Number.Should().Be(SecondCycle);
        routine.Cycle.StartKm.Should().Be(ServicedKm);
        routine.Cycle.StartDate.Should().Be(PlanTestFactory.LocalDateOfLateUtcInstant());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnPlanNotFound_When_PlanDoesNotExist()
    {
        RestartRoutineCommand command = new(VehicleId.New(), RoutineId.New(), ServicedKm, PlanTestFactory.LateUtcInstant(), Guid.NewGuid());
        _plans.GetAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(null));

        Result<RoutineId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.MaintenancePlanNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnRoutineNotFound_When_PlanHasNoSuchRoutine()
    {
        MaintenancePlan plan = PlanTestFactory.Plan(VehicleId.New(), MaintenancePlanStatus.Active);
        _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(plan));
        RestartRoutineCommand command = new(plan.Id, RoutineId.New(), ServicedKm, PlanTestFactory.LateUtcInstant(), Guid.NewGuid());

        Result<RoutineId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.RoutineNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
