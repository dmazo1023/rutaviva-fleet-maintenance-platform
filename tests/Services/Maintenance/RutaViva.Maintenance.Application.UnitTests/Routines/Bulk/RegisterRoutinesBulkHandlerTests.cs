using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Routines.Bulk;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.Application.UnitTests.Routines.Bulk;

public sealed class RegisterRoutinesBulkHandlerTests
{
    private const string ExistingPlate = "RVA101";
    private const string EmptyPlate = "RVA102";
    private const string UnknownPlate = "ZZZ999";
    private const string MalformedPlate = "12";
    private const string UnknownTypeCode = "ENGINE_X";
    private const string LicensePlateInvalidCode = "LicensePlate.Invalid";
    private const string ThresholdOutOfRangeCode = "DueSoonThreshold.OutOfRange";
    private const string SaveFailureCode = "Test.SaveFailed";
    private const string SaveFailureMessage = "The save failed.";
    private const long KilometersAboveOdometer = 35000;
    private const long KilometersBelowOdometer = 20000;
    private const int InvalidPercent = 100;
    private const int FirstRowNumber = 1;
    private const int SecondRowNumber = 2;
    private const int ThirdRowNumber = 3;
    private const int ExpectedLookups = 1;
    private const int ExpectedSaveCalls = 1;
    private const int OverLimitRows = RegisterRoutinesBulkCommandValidator.MaxRows + 1;
    private const int LastServiceYear = 2026;
    private const int LastServiceMonth = 6;
    private const int LastServiceDay = 1;

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(RoutineTestData.Now);
    private readonly RoutineParameterCatalog _catalog = RoutineTestData.Catalog();
    private readonly MaintenancePlan _existingPlan = RoutineTestData.Plan(ExistingPlate);
    private readonly MaintenancePlan _emptyPlan = RoutineTestData.Plan(EmptyPlate);

    public RegisterRoutinesBulkHandlerTests()
    {
        RoutineTestData.AddRoutine(_existingPlan, RoutineTestData.OilChangeId, _catalog);
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(_catalog);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _plans.GetByPlatesAsync(Arg.Any<IReadOnlyCollection<LicensePlate>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(LoadedPlans()));
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidation_When_RowsExceedMaximum()
    {
        List<RoutineBulkRow> rows = Enumerable.Range(FirstRowNumber, OverLimitRows)
            .Select(number => Row(number, EmptyPlate, RoutineTestData.OilChangeCode))
            .ToList();

        Result<IReadOnlyList<BulkRowResult>> result = await CreateDecorated()
            .HandleAsync(new RegisterRoutinesBulkCommand(rows, false), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _plans.DidNotReceive().GetByPlatesAsync(Arg.Any<IReadOnlyCollection<LicensePlate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_NotCallSaveChanges_When_DryRun()
    {
        RoutineBulkRow row = Row(FirstRowNumber, EmptyPlate, RoutineTestData.OilChangeCode);

        IReadOnlyList<BulkRowResult> results = await ExecuteAsync(true, row);

        results.Single().Outcome.Should().Be(BulkRowOutcome.Valid);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_SaveOnce_When_NotDryRun()
    {
        RoutineBulkRow row = Row(FirstRowNumber, EmptyPlate, RoutineTestData.OilChangeCode);

        IReadOnlyList<BulkRowResult> results = await ExecuteAsync(false, row);

        results.Single().Outcome.Should().Be(BulkRowOutcome.Valid);
        _emptyPlan.Routines.Should().ContainSingle();
        await _unitOfWork.Received(ExpectedSaveCalls).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_LoadPlansAndCatalogOnce_When_ManyRows()
    {
        RoutineBulkRow first = Row(FirstRowNumber, EmptyPlate, RoutineTestData.OilChangeCode);
        RoutineBulkRow second = Row(SecondRowNumber, EmptyPlate, RoutineTestData.BrakesCode);
        RoutineBulkRow third = Row(ThirdRowNumber, ExistingPlate, RoutineTestData.BrakesCode);

        await ExecuteAsync(true, first, second, third);

        await _plans.Received(ExpectedLookups).GetByPlatesAsync(Arg.Any<IReadOnlyCollection<LicensePlate>>(), Arg.Any<CancellationToken>());
        await _routineTypes.Received(ExpectedLookups).GetCatalogAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(UnknownPlate, RoutineTestData.OilChangeCode, null, false, BulkErrorCodes.MaintenancePlanNotFound)]
    [InlineData(EmptyPlate, UnknownTypeCode, null, false, BulkErrorCodes.RoutineTypeNotFound)]
    [InlineData(EmptyPlate, RoutineTestData.BrakesCode, KilometersAboveOdometer, true, BulkErrorCodes.RoutineLastServiceKmAboveOdometer)]
    [InlineData(EmptyPlate, RoutineTestData.BrakesCode, KilometersBelowOdometer, false, BulkErrorCodes.RoutineIncompleteLastService)]
    [InlineData(EmptyPlate, RoutineTestData.BrakesCode, null, true, BulkErrorCodes.RoutineIncompleteLastService)]
    [InlineData(MalformedPlate, RoutineTestData.BrakesCode, null, false, LicensePlateInvalidCode)]
    public async Task HandleAsync_Should_ReturnInvalidWithCode_When_RowBreaksBusinessRule(
        string plate,
        string routineTypeCode,
        long? lastServiceKm,
        bool hasLastServiceDate,
        string expectedCode)
    {
        DateOnly? lastServiceDate = hasLastServiceDate ? new DateOnly(LastServiceYear, LastServiceMonth, LastServiceDay) : null;
        RoutineBulkRow row = Row(FirstRowNumber, plate, routineTypeCode, lastServiceKm, lastServiceDate);

        IReadOnlyList<BulkRowResult> results = await ExecuteAsync(false, row);

        BulkRowResult result = results.Single();
        result.Outcome.Should().Be(BulkRowOutcome.Invalid);
        result.Errors.Select(error => error.Code).Should().ContainSingle().Which.Should().Be(expectedCode);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidWithValidationCode_When_PercentIsOutOfRange()
    {
        RoutineBulkRow row = Row(FirstRowNumber, EmptyPlate, RoutineTestData.OilChangeCode, dueSoonPercent: InvalidPercent);

        IReadOnlyList<BulkRowResult> results = await ExecuteAsync(false, row);

        results.Single().Errors.Single().Code.Should().Be(ThresholdOutOfRangeCode);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSkipped_When_RoutineAlreadyExists()
    {
        RoutineBulkRow row = Row(FirstRowNumber, ExistingPlate, RoutineTestData.OilChangeCode);

        IReadOnlyList<BulkRowResult> results = await ExecuteAsync(false, row);

        BulkRowResult result = results.Single();
        result.Outcome.Should().Be(BulkRowOutcome.Skipped);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnDuplicateInBatch_When_SecondRowRepeatsPlateAndCode()
    {
        RoutineBulkRow first = Row(FirstRowNumber, EmptyPlate, RoutineTestData.BrakesCode);
        RoutineBulkRow second = Row(SecondRowNumber, EmptyPlate, RoutineTestData.BrakesCode);

        IReadOnlyList<BulkRowResult> results = await ExecuteAsync(false, first, second);

        results[0].Outcome.Should().Be(BulkRowOutcome.Valid);
        results[1].Outcome.Should().Be(BulkRowOutcome.Invalid);
        results[1].Errors.Single().Code.Should().Be(BulkErrorCodes.DuplicateInBatch);
        _emptyPlan.Routines.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSaveError_When_SaveChangesFails()
    {
        Error failure = new(SaveFailureCode, SaveFailureMessage, ErrorType.Conflict);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(failure));
        RoutineBulkRow row = Row(FirstRowNumber, EmptyPlate, RoutineTestData.OilChangeCode);

        Result<IReadOnlyList<BulkRowResult>> result = await CreateHandler()
            .HandleAsync(new RegisterRoutinesBulkCommand([row], false), TestContext.Current.CancellationToken);

        result.Error.Should().Be(failure);
    }

    private static RoutineBulkRow Row(
        int rowNumber,
        string plate,
        string routineTypeCode,
        long? lastServiceKm = null,
        DateOnly? lastServiceDate = null,
        int? dueSoonPercent = null) =>
        new(rowNumber, plate, routineTypeCode, lastServiceKm, lastServiceDate, null, null, dueSoonPercent);

    private IReadOnlyDictionary<LicensePlate, MaintenancePlan> LoadedPlans() =>
        new Dictionary<LicensePlate, MaintenancePlan>
        {
            [_existingPlan.Plate] = _existingPlan,
            [_emptyPlan.Plate] = _emptyPlan
        };

    private async Task<IReadOnlyList<BulkRowResult>> ExecuteAsync(bool dryRun, params RoutineBulkRow[] rows)
    {
        Result<IReadOnlyList<BulkRowResult>> result = await CreateHandler()
            .HandleAsync(new RegisterRoutinesBulkCommand(rows, dryRun), TestContext.Current.CancellationToken);
        return result.Value;
    }

    private RegisterRoutinesBulkHandler CreateHandler() => new(_plans, _routineTypes, _unitOfWork, _clock, new BulkRoutineRowValidator());

    private ValidationCommandHandlerDecorator<RegisterRoutinesBulkCommand, IReadOnlyList<BulkRowResult>> CreateDecorated() =>
        new(CreateHandler(), [new RegisterRoutinesBulkCommandValidator()]);
}
