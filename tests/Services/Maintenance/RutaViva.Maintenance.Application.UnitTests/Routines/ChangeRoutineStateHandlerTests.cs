using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.Application.UnitTests.Routines;

public sealed class ChangeRoutineStateHandlerTests
{
    private const string Plate = "RVA101";
    private const string Editor = "ops.user";
    private const string BlankEditor = "";
    private const int ExpectedSaveCalls = 1;

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(RoutineTestData.Now);
    private readonly RoutineParameterCatalog _catalog = RoutineTestData.Catalog();

    public ChangeRoutineStateHandlerTests()
    {
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(_catalog);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_PlanDoesNotExist()
    {
        ChangeRoutineStateCommand command = new(VehicleId.New(), RoutineId.New(), RoutineState.Inactive, Editor);
        _plans.GetAsync(command.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(null));

        Result<RoutineId> result = await CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.MaintenancePlanNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_SaveOnce_When_CommandIsValid()
    {
        MaintenancePlan plan = RoutineTestData.Plan(Plate);
        RoutineId routineId = RoutineTestData.AddRoutine(plan, RoutineTestData.OilChangeId, _catalog);
        _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(plan));
        ChangeRoutineStateCommand command = new(plan.Id, routineId, RoutineState.Inactive, Editor);

        Result<RoutineId> result = await CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(routineId);
        plan.Routines.Single().State.Should().Be(RoutineState.Inactive);
        await _unitOfWork.Received(ExpectedSaveCalls).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnRoutineNotFound_When_RoutineDoesNotBelongToPlan()
    {
        MaintenancePlan plan = RoutineTestData.Plan(Plate);
        _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(plan));
        ChangeRoutineStateCommand command = new(plan.Id, RoutineId.New(), RoutineState.Inactive, Editor);

        Result<RoutineId> result = await CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.RoutineNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidation_When_UpdatedByIsEmpty()
    {
        ChangeRoutineStateCommand command = new(VehicleId.New(), RoutineId.New(), RoutineState.Inactive, BlankEditor);

        Result<RoutineId> result = await CreateDecorated().HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _plans.DidNotReceive().GetAsync(Arg.Any<VehicleId>(), Arg.Any<CancellationToken>());
    }

    private ChangeRoutineStateHandler CreateHandler() => new(_plans, _routineTypes, _unitOfWork, _clock);

    private ValidationCommandHandlerDecorator<ChangeRoutineStateCommand, RoutineId> CreateDecorated() =>
        new(CreateHandler(), [new ChangeRoutineStateCommandValidator()]);
}
