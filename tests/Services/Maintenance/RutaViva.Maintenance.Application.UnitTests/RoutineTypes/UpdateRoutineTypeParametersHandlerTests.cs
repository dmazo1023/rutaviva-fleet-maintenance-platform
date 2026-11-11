using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.RoutineTypes;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.Events;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.UnitTests.RoutineTypes;

public sealed class UpdateRoutineTypeParametersHandlerTests
{
    private const string Code = "OIL_CHANGE";
    private const string Name = "Oil change";
    private const string SeedUser = "seed";
    private const string UpdatedBy = "ops.user";
    private const string NotFoundCode = "RoutineType.NotFound";
    private const string IntervalEmptyCode = "MaintenanceInterval.Empty";
    private const string ConflictCode = "Concurrency.Conflict";
    private const string ConflictMessage = "Conflict";
    private const int InitialKm = 10000;
    private const int InitialMonths = 6;
    private const int NewKm = 20000;
    private const int NewMonths = 12;
    private const decimal InitialPercent = 10m;
    private const decimal NewPercent = 15m;
    private const int SeedYear = 2026;
    private const int NowMonth = 11;
    private const int NowDay = 12;
    private const int SeedMonth = 1;
    private const int SeedDay = 1;

    private static readonly DateTimeOffset SeedAt = new(SeedYear, SeedMonth, SeedDay, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(SeedYear, NowMonth, NowDay, 10, 0, 0, TimeSpan.Zero);

    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly UpdateRoutineTypeParametersHandler _handler;

    public UpdateRoutineTypeParametersHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new UpdateRoutineTypeParametersHandler(_routineTypes, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_UpdateParametersAndSave_When_RoutineTypeExists()
    {
        RoutineType routineType = CreateRoutineType();
        _routineTypes.GetAsync(routineType.Id, Arg.Any<CancellationToken>()).Returns(routineType);

        Result<RoutineTypeId> result = await _handler.HandleAsync(CommandFor(routineType.Id, NewKm, NewMonths, NewPercent), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(routineType.Id);
        routineType.DefaultInterval.Kilometers.Should().Be(NewKm);
        routineType.DefaultInterval.Months.Should().Be(NewMonths);
        routineType.DueSoonThreshold.Percent.Should().Be(NewPercent);
        routineType.UpdatedBy.Should().Be(UpdatedBy);
        routineType.UpdatedAt.Should().Be(Now);
        routineType.DomainEvents.OfType<RoutineTypeParametersChanged>().Should().ContainSingle();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_RoutineTypeDoesNotExist()
    {
        RoutineTypeId id = RoutineTypeId.New();
        _routineTypes.GetAsync(id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<RoutineType?>(null));

        Result<RoutineTypeId> result = await _handler.HandleAsync(CommandFor(id, NewKm, NewMonths, NewPercent), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NotFoundCode);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnIntervalError_When_NoDimensionIsGiven()
    {
        RoutineType routineType = CreateRoutineType();
        _routineTypes.GetAsync(routineType.Id, Arg.Any<CancellationToken>()).Returns(routineType);

        Result<RoutineTypeId> result = await _handler.HandleAsync(CommandFor(routineType.Id, null, null, NewPercent), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(IntervalEmptyCode);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSaveError_When_SaveFails()
    {
        RoutineType routineType = CreateRoutineType();
        _routineTypes.GetAsync(routineType.Id, Arg.Any<CancellationToken>()).Returns(routineType);
        Error conflict = new(ConflictCode, ConflictMessage, ErrorType.Conflict);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(conflict));

        Result<RoutineTypeId> result = await _handler.HandleAsync(CommandFor(routineType.Id, NewKm, NewMonths, NewPercent), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(conflict);
    }

    private static UpdateRoutineTypeParametersCommand CommandFor(RoutineTypeId id, int? kilometers, int? months, decimal percent) =>
        new(id, kilometers, months, percent, UpdatedBy);

    private static RoutineType CreateRoutineType() =>
        RoutineType.Create(
            RoutineTypeId.New(),
            RoutineTypeCode.Create(Code).Value,
            Name,
            MaintenanceInterval.Create(InitialKm, InitialMonths).Value,
            DueSoonThreshold.Create(InitialPercent).Value,
            SeedUser,
            SeedAt).Value;
}
