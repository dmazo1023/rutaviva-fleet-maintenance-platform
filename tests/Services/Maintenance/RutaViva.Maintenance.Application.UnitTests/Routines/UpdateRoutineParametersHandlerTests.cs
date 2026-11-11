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

public sealed class UpdateRoutineParametersHandlerTests
{
    private const string Plate = "RVA101";
    private const string Editor = "ops.user";
    private const string BlankEditor = " ";
    private const int ValidIntervalKm = 4000;
    private const int ValidIntervalMonths = 3;
    private const decimal ValidPercent = 20m;
    private const int InvalidInterval = 0;
    private const decimal ZeroPercent = 0m;
    private const decimal FullPercent = 100m;
    private const int ExpectedSaveCalls = 1;

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(RoutineTestData.Now);
    private readonly RoutineParameterCatalog _catalog = RoutineTestData.Catalog();

    public UpdateRoutineParametersHandlerTests()
    {
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(_catalog);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    public static TheoryData<int?, int?, decimal?, string> InvalidInputs => new()
    {
        { InvalidInterval, null, null, Editor },
        { null, InvalidInterval, null, Editor },
        { null, null, ZeroPercent, Editor },
        { null, null, FullPercent, Editor },
        { null, null, null, BlankEditor }
    };

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_PlanDoesNotExist()
    {
        UpdateRoutineParametersCommand command = new(VehicleId.New(), RoutineId.New(), ValidIntervalKm, null, null, Editor);
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
        UpdateRoutineParametersCommand command = new(plan.Id, routineId, ValidIntervalKm, ValidIntervalMonths, ValidPercent, Editor);

        Result<RoutineId> result = await CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(routineId);
        plan.Routines.Single().Overrides.DueSoonPercent.Should().Be(ValidPercent);
        await _unitOfWork.Received(ExpectedSaveCalls).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnRoutineNotFound_When_RoutineDoesNotBelongToPlan()
    {
        MaintenancePlan plan = RoutineTestData.Plan(Plate);
        _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(plan));
        UpdateRoutineParametersCommand command = new(plan.Id, RoutineId.New(), ValidIntervalKm, null, null, Editor);

        Result<RoutineId> result = await CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.RoutineNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public async Task HandleAsync_Should_ReturnValidation_When_InputIsInvalid(
        int? intervalKm,
        int? intervalMonths,
        decimal? percent,
        string updatedBy)
    {
        UpdateRoutineParametersCommand command = new(VehicleId.New(), RoutineId.New(), intervalKm, intervalMonths, percent, updatedBy);

        Result<RoutineId> result = await CreateDecorated().HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _plans.DidNotReceive().GetAsync(Arg.Any<VehicleId>(), Arg.Any<CancellationToken>());
    }

    private UpdateRoutineParametersHandler CreateHandler() => new(_plans, _routineTypes, _unitOfWork, _clock);

    private ValidationCommandHandlerDecorator<UpdateRoutineParametersCommand, RoutineId> CreateDecorated() =>
        new(CreateHandler(), [new UpdateRoutineParametersCommandValidator()]);
}
