using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.Application.UnitTests.Plans;

public sealed class ChangePlanStatusHandlerTests
{
    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(PlanTestFactory.LateUtcInstant());
    private readonly ChangePlanStatusHandler _handler;

    public ChangePlanStatusHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(PlanTestFactory.Catalog()));
        _handler = new ChangePlanStatusHandler(_plans, _routineTypes, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_SuspendPlanAndSave_When_TargetIsSuspended()
    {
        MaintenancePlan plan = StubPlan(MaintenancePlanStatus.Active);
        ChangePlanStatusCommand command = new(plan.Id, MaintenancePlanStatus.Suspended);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(MaintenancePlanStatus.Suspended);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ResumePlanAndSave_When_TargetIsActive()
    {
        MaintenancePlan plan = StubPlan(MaintenancePlanStatus.Suspended);
        ChangePlanStatusCommand command = new(plan.Id, MaintenancePlanStatus.Active);

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(MaintenancePlanStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_PlanDoesNotExist()
    {
        ChangePlanStatusCommand command = new(VehicleId.New(), MaintenancePlanStatus.Suspended);
        _plans.GetAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(null));

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.MaintenancePlanNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private MaintenancePlan StubPlan(MaintenancePlanStatus status)
    {
        MaintenancePlan plan = PlanTestFactory.Plan(VehicleId.New(), status);
        _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(plan));
        return plan;
    }
}
