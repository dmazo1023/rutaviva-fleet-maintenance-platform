using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Evaluations;
using RutaViva.Maintenance.Application.UnitTests.Telemetry;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.UnitTests.Evaluations;

public sealed class ReevaluateMaintenancePlansHandlerTests
{
    private const int TotalPlans = 250;
    private const int ThreePages = 3;
    private const int PageSize = 100;
    private const int FirstPageLastIndex = 99;
    private const int SecondPageLastIndex = 199;
    private const int ThreePlansCount = 3;
    private const int TwoPlansCount = 2;
    private const int OnePlanCount = 1;
    private const int OneCall = 1;
    private const int NoCount = 0;
    private const int NotFoundIndex = -1;
    private const int ExpectedFailureIndex = 1;
    private const string FailureMarker = "TimeEvaluationFailed";
    private const string FailureMessage = "Evaluation failed.";
    private const string SaveFailureCode = "Concurrency.Conflict";
    private const string SaveFailureMessage = "The record was modified by another process.";

    private readonly IUnitOfWorkScopeFactory _scopeFactory = Substitute.For<IUnitOfWorkScopeFactory>();
    private readonly IUnitOfWorkScope _scope = Substitute.For<IUnitOfWorkScope>();
    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly CapturingLogger<ReevaluateMaintenancePlansHandler> _logger = new();
    private readonly ReevaluateMaintenancePlansHandler _handler;

    public ReevaluateMaintenancePlansHandlerTests()
    {
        _scopeFactory.Create().Returns(_scope);
        _scope.Plans.Returns(_plans);
        _scope.RoutineTypes.Returns(_routineTypes);
        _scope.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(EvaluationTestData.Catalog());
        _handler = new ReevaluateMaintenancePlansHandler(_scopeFactory, _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_RequestThreePagesOfOneHundred_When_ThereAreTwoHundredFiftyPlans()
    {
        List<VehicleId> ids = ArrangePagedPlans(TotalPlans);

        Result<ReevaluationSummary> result = await _handler.HandleAsync(CommandForAll(), TestContext.Current.CancellationToken);

        result.Value.EvaluatedPlans.Should().Be(TotalPlans);
        await _plans.Received(ThreePages).GetActivePlanIdsPageAsync(Arg.Any<VehicleId?>(), PageSize, Arg.Any<CancellationToken>());
        await _plans.Received(OneCall).GetActivePlanIdsPageAsync(null, PageSize, Arg.Any<CancellationToken>());
        await _plans.Received(OneCall).GetActivePlanIdsPageAsync(ids[FirstPageLastIndex], PageSize, Arg.Any<CancellationToken>());
        await _plans.Received(OneCall).GetActivePlanIdsPageAsync(ids[SecondPageLastIndex], PageSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_SaveOncePerPlan_When_PlansAreEvaluated()
    {
        ArrangePagedPlans(TotalPlans);

        await _handler.HandleAsync(CommandForAll(), TestContext.Current.CancellationToken);

        await _scope.Received(TotalPlans).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_LoadCatalogOnce_When_ManyPlansAreEvaluated()
    {
        ArrangePagedPlans(TotalPlans);

        await _handler.HandleAsync(CommandForAll(), TestContext.Current.CancellationToken);

        await _routineTypes.Received(OneCall).GetCatalogAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnEmptySummary_When_ThereAreNoActivePlans()
    {
        ArrangePagedPlans(NoCount);

        Result<ReevaluationSummary> result = await _handler.HandleAsync(CommandForAll(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new ReevaluationSummary(NoCount, NoCount, NoCount, NoCount));
    }

    [Fact]
    public async Task HandleAsync_Should_ContinueAndLog_When_OnePlanThrows()
    {
        List<VehicleId> ids = ArrangePagedPlans(ThreePlansCount);
        _plans.GetAsync(ids[ExpectedFailureIndex], Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException(FailureMessage));

        Result<ReevaluationSummary> result = await _handler.HandleAsync(CommandForAll(), TestContext.Current.CancellationToken);

        result.Value.EvaluatedPlans.Should().Be(TwoPlansCount);
        CapturedLog entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Message.Should().Contain(FailureMarker).And.Contain(ids[ExpectedFailureIndex].Value.ToString());
        entry.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_Should_ContinueAndLog_When_OnePlanFailsToSave()
    {
        List<VehicleId> ids = ArrangePagedPlans(ThreePlansCount);
        Error saveFailure = new(SaveFailureCode, SaveFailureMessage, ErrorType.Conflict);
        _scope.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(), Result.Failure(saveFailure), Result.Success());

        Result<ReevaluationSummary> result = await _handler.HandleAsync(CommandForAll(), TestContext.Current.CancellationToken);

        result.Value.EvaluatedPlans.Should().Be(TwoPlansCount);
        CapturedLog entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain(FailureMarker).And.Contain(ids[ExpectedFailureIndex].Value.ToString()).And.Contain(SaveFailureCode);
    }

    [Fact]
    public async Task HandleAsync_Should_SumOutcomes_When_PlansBecomeDueSoonAndOverdue()
    {
        MaintenancePlan dueSoonPlan = EvaluationTestData.PlanWithRoutineServicedOn(EvaluationTestData.DueSoonServiceDateText);
        MaintenancePlan overduePlan = EvaluationTestData.PlanWithRoutineServicedOn(EvaluationTestData.OverdueServiceDateText);
        MaintenancePlan quietPlan = EvaluationTestData.PlanWithoutRoutines();
        ArrangePlans(dueSoonPlan, overduePlan, quietPlan);

        Result<ReevaluationSummary> result = await _handler.HandleAsync(CommandForAll(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(new ReevaluationSummary(ThreePlansCount, OnePlanCount, OnePlanCount, OnePlanCount));
    }

    [Fact]
    public async Task HandleAsync_Should_EvaluateOnlyThatPlan_When_VehicleIdIsGiven()
    {
        MaintenancePlan overduePlan = EvaluationTestData.PlanWithRoutineServicedOn(EvaluationTestData.OverdueServiceDateText);
        _plans.GetAsync(overduePlan.Id, Arg.Any<CancellationToken>()).Returns(overduePlan);
        ReevaluateMaintenancePlansCommand command = new(EvaluationTestData.AsOfDate, overduePlan.Id);

        Result<ReevaluationSummary> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Value.Should().Be(new ReevaluationSummary(OnePlanCount, NoCount, OnePlanCount, OnePlanCount));
        await _plans.DidNotReceive().GetActivePlanIdsPageAsync(Arg.Any<VehicleId?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _scope.Received(OneCall).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_VehicleIdHasNoPlan()
    {
        ReevaluateMaintenancePlansCommand command = new(EvaluationTestData.AsOfDate, VehicleId.New());

        Result<ReevaluationSummary> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.MaintenancePlanNotFound);
    }

    private static ReevaluateMaintenancePlansCommand CommandForAll() => new(EvaluationTestData.AsOfDate, null);

    private static IReadOnlyList<VehicleId> PageAfter(IReadOnlyList<VehicleId> ids, VehicleId? after)
    {
        int start = after is { } cursor ? IndexOf(ids, cursor) + OneCall : NoCount;
        return ids.Skip(start).Take(PageSize).ToList();
    }

    private static int IndexOf(IReadOnlyList<VehicleId> ids, VehicleId id)
    {
        for (int index = NoCount; index < ids.Count; index++)
        {
            if (ids[index] == id)
            {
                return index;
            }
        }

        return NotFoundIndex;
    }

    private List<VehicleId> ArrangePagedPlans(int count)
    {
        List<VehicleId> ids = Enumerable.Range(NoCount, count).Select(_ => VehicleId.New()).ToList();
        _plans.GetActivePlanIdsPageAsync(Arg.Any<VehicleId?>(), PageSize, Arg.Any<CancellationToken>())
            .Returns(call => PageAfter(ids, call.ArgAt<VehicleId?>(0)));
        MaintenancePlan plan = EvaluationTestData.PlanWithoutRoutines();
        _plans.GetAsync(Arg.Any<VehicleId>(), Arg.Any<CancellationToken>()).Returns(plan);
        return ids;
    }

    private void ArrangePlans(params MaintenancePlan[] plans)
    {
        List<VehicleId> ids = plans.Select(plan => plan.Id).ToList();
        _plans.GetActivePlanIdsPageAsync(Arg.Any<VehicleId?>(), PageSize, Arg.Any<CancellationToken>())
            .Returns(call => PageAfter(ids, call.ArgAt<VehicleId?>(0)));
        foreach (MaintenancePlan plan in plans)
        {
            _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        }
    }
}
