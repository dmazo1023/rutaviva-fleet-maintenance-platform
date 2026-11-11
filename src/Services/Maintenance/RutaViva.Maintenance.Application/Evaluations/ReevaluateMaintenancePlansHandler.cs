using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.Evaluations;

public sealed class ReevaluateMaintenancePlansHandler(
    IUnitOfWorkScopeFactory scopeFactory,
    ILogger<ReevaluateMaintenancePlansHandler> logger)
    : ICommandHandler<ReevaluateMaintenancePlansCommand, ReevaluationSummary>
{
    public const int PageSize = 100;

    private const int NoCount = 0;
    private const int CountStep = 1;
    private const string ExceptionFailedTemplate = "TimeEvaluationFailed for vehicle {VehicleId}";
    private const string ErrorFailedTemplate = "TimeEvaluationFailed for vehicle {VehicleId} with {ErrorCode}";

    private static readonly ReevaluationSummary EmptySummary = new(NoCount, NoCount, NoCount, NoCount);

    public async Task<Result<ReevaluationSummary>> HandleAsync(
        ReevaluateMaintenancePlansCommand command,
        CancellationToken cancellationToken)
    {
        RoutineParameterCatalog catalog = await LoadCatalogAsync(cancellationToken);
        if (command.VehicleId is { } vehicleId)
        {
            return await ReevaluateOneAsync(vehicleId, command.AsOfDate, catalog, cancellationToken);
        }

        return await ReevaluateAllAsync(command.AsOfDate, catalog, cancellationToken);
    }

    private static ReevaluationSummary Accumulate(ReevaluationSummary summary, ReevaluationOutcome outcome) =>
        new(
            summary.EvaluatedPlans + CountStep,
            summary.DueSoon + outcome.NewDueSoon,
            summary.Overdue + outcome.NewOverdue,
            summary.OrdersRequested + outcome.NewOverdue);

    private async Task<RoutineParameterCatalog> LoadCatalogAsync(CancellationToken cancellationToken)
    {
        await using IUnitOfWorkScope scope = scopeFactory.Create();
        return await scope.RoutineTypes.GetCatalogAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<VehicleId>> LoadPageAsync(VehicleId? after, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkScope scope = scopeFactory.Create();
        return await scope.Plans.GetActivePlanIdsPageAsync(after, PageSize, cancellationToken);
    }

    private async Task<Result<ReevaluationSummary>> ReevaluateOneAsync(
        VehicleId vehicleId,
        DateOnly asOfDate,
        RoutineParameterCatalog catalog,
        CancellationToken cancellationToken)
    {
        Result<ReevaluationOutcome>? evaluation = await TryEvaluateAsync(vehicleId, asOfDate, catalog, cancellationToken);
        if (evaluation is null)
        {
            return EmptySummary;
        }

        if (evaluation.IsFailure)
        {
            return evaluation.Error;
        }

        return Accumulate(EmptySummary, evaluation.Value);
    }

    private async Task<ReevaluationSummary> ReevaluateAllAsync(
        DateOnly asOfDate,
        RoutineParameterCatalog catalog,
        CancellationToken cancellationToken)
    {
        ReevaluationSummary summary = EmptySummary;
        IReadOnlyList<VehicleId> page = await LoadPageAsync(null, cancellationToken);
        while (page.Count > NoCount)
        {
            summary = await EvaluatePageAsync(page, asOfDate, catalog, summary, cancellationToken);
            page = await LoadNextPageAsync(page, cancellationToken);
        }

        return summary;
    }

    private async Task<IReadOnlyList<VehicleId>> LoadNextPageAsync(
        IReadOnlyList<VehicleId> page,
        CancellationToken cancellationToken)
    {
        if (page.Count < PageSize)
        {
            return [];
        }

        return await LoadPageAsync(page[^CountStep], cancellationToken);
    }

    private async Task<ReevaluationSummary> EvaluatePageAsync(
        IReadOnlyList<VehicleId> page,
        DateOnly asOfDate,
        RoutineParameterCatalog catalog,
        ReevaluationSummary summary,
        CancellationToken cancellationToken)
    {
        ReevaluationSummary accumulated = summary;
        foreach (VehicleId vehicleId in page)
        {
            Result<ReevaluationOutcome>? evaluation = await TryEvaluateAsync(vehicleId, asOfDate, catalog, cancellationToken);
            if (evaluation is { IsSuccess: true })
            {
                accumulated = Accumulate(accumulated, evaluation.Value);
            }
        }

        return accumulated;
    }

    private async Task<Result<ReevaluationOutcome>?> TryEvaluateAsync(
        VehicleId vehicleId,
        DateOnly asOfDate,
        RoutineParameterCatalog catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            Result<ReevaluationOutcome> evaluation = await EvaluatePlanAsync(vehicleId, asOfDate, catalog, cancellationToken);
            LogIfFailed(vehicleId, evaluation);
            return evaluation;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, ExceptionFailedTemplate, vehicleId.Value);
            return null;
        }
    }

    private async Task<Result<ReevaluationOutcome>> EvaluatePlanAsync(
        VehicleId vehicleId,
        DateOnly asOfDate,
        RoutineParameterCatalog catalog,
        CancellationToken cancellationToken)
    {
        await using IUnitOfWorkScope scope = scopeFactory.Create();
        MaintenancePlan? plan = await scope.Plans.GetAsync(vehicleId, cancellationToken);
        if (plan is null)
        {
            return MaintenanceErrors.MaintenancePlanNotFound;
        }

        ReevaluationOutcome outcome = plan.Reevaluate(asOfDate, catalog);
        Result saved = await scope.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return outcome;
    }

    private void LogIfFailed(VehicleId vehicleId, Result<ReevaluationOutcome> evaluation)
    {
        if (evaluation.IsSuccess)
        {
            return;
        }

        logger.LogWarning(ErrorFailedTemplate, vehicleId.Value, evaluation.Error.Code);
    }
}
