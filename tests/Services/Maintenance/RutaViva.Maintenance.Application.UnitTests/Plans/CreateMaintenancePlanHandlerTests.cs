using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.UnitTests.Plans;

public sealed class CreateMaintenancePlanHandlerTests
{
    private const string InvalidPlate = "??";

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateMaintenancePlanHandler _handler;

    public CreateMaintenancePlanHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new CreateMaintenancePlanHandler(_plans, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_AddPlanAndSave_When_PlanDoesNotExist()
    {
        CreateMaintenancePlanCommand command = CreateCommand(PlanTestFactory.PlateValue);
        _plans.GetAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(null));

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(command.VehicleId);
        _plans.Received(1).Add(Arg.Is<MaintenancePlan>(plan => IsPlanOf(plan, command)));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccessWithoutDuplicating_When_PlanAlreadyExists()
    {
        CreateMaintenancePlanCommand command = CreateCommand(PlanTestFactory.PlateValue);
        MaintenancePlan existing = PlanTestFactory.Plan(command.VehicleId, MaintenancePlanStatus.Active);
        _plans.GetAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(existing));

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _plans.DidNotReceive().Add(Arg.Any<MaintenancePlan>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnPlateError_When_PlateIsInvalid()
    {
        CreateMaintenancePlanCommand command = CreateCommand(InvalidPlate);
        _plans.GetAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(null));

        Result<VehicleId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.LicensePlateInvalid);
        _plans.DidNotReceive().Add(Arg.Any<MaintenancePlan>());
    }

    private static bool IsPlanOf(MaintenancePlan plan, CreateMaintenancePlanCommand command) =>
        plan.Id == command.VehicleId
        && plan.Plate.Value == command.Plate
        && plan.CustomerId == command.CustomerId
        && plan.Status == command.Status
        && plan.Odometer.Kilometers == command.OdometerKm
        && plan.LastTimeEvaluationDate == PlanTestFactory.LocalDateOfLateUtcInstant();

    private static CreateMaintenancePlanCommand CreateCommand(string plate) =>
        new(
            VehicleId.New(),
            plate,
            CustomerId.New(),
            MaintenancePlanStatus.Active,
            PlanTestFactory.OdometerKm,
            PlanTestFactory.LateUtcInstant());
}
