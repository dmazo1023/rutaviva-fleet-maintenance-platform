using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Evaluations;

namespace RutaViva.Maintenance.Infrastructure.Scheduling;

public sealed class DailyTimeEvaluationScheduler(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    TimeProvider timeProvider,
    TimeEvaluationScheduleOptions options,
    ILogger<DailyTimeEvaluationScheduler> logger) : BackgroundService
{
    private const string NextRunTemplate = "TimeEvaluationScheduled for {NextRunUtc}";
    private const string CompletedTemplate =
        "TimeEvaluationCompleted for {AsOfDate} with {EvaluatedPlans} plans, {DueSoon} due soon and {Overdue} overdue";

    private const string RejectedTemplate = "TimeEvaluationRejected for {AsOfDate} with {ErrorCode}";
    private const string FailedTemplate = "TimeEvaluationFailed for {AsOfDate}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        await DispatchSafelyAsync(timeZone, stoppingToken);
        while (await WaitForNextRunAsync(timeZone, stoppingToken))
        {
            await DispatchSafelyAsync(timeZone, stoppingToken);
        }
    }

    private async Task<bool> WaitForNextRunAsync(TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        DateTimeOffset nextRun = NextRunCalculator.Calculate(clock.UtcNow, options.RunAt, timeZone);
        logger.LogInformation(NextRunTemplate, nextRun);
        try
        {
            await Task.Delay(nextRun - clock.UtcNow, timeProvider, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task DispatchSafelyAsync(TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        DateOnly today = clock.TodayIn(timeZone);
        try
        {
            await DispatchAsync(today, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, FailedTemplate, today);
        }
    }

    private async Task DispatchAsync(DateOnly today, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ICommandHandler<ReevaluateMaintenancePlansCommand, ReevaluationSummary> handler =
            scope.ServiceProvider.GetRequiredService<ICommandHandler<ReevaluateMaintenancePlansCommand, ReevaluationSummary>>();
        Result<ReevaluationSummary> result = await handler.HandleAsync(
            new ReevaluateMaintenancePlansCommand(today, null),
            cancellationToken);
        LogResult(today, result);
    }

    private void LogResult(DateOnly today, Result<ReevaluationSummary> result)
    {
        if (result.IsFailure)
        {
            logger.LogError(RejectedTemplate, today, result.Error.Code);
            return;
        }

        logger.LogInformation(
            CompletedTemplate,
            today,
            result.Value.EvaluatedPlans,
            result.Value.DueSoon,
            result.Value.Overdue);
    }
}
