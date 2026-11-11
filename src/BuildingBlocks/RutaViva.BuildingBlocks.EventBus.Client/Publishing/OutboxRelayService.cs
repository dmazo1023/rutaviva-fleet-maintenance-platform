using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RutaViva.BuildingBlocks.EventBus.Client.Publishing;

public sealed class OutboxRelayService<TContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    public const int PollIntervalMilliseconds = 500;

    private const string CycleFailedTemplate = "Outbox relay cycle for {ContextName} failed and will be retried";

    private static readonly string ContextName = typeof(TContext).Name;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(PollIntervalMilliseconds));
        do
        {
            await RunCycleAsync(stoppingToken);
        }
        while (await WaitNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            OutboxBatchProcessor<TContext> processor = scope.ServiceProvider.GetRequiredService<OutboxBatchProcessor<TContext>>();
            await processor.ProcessAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, CycleFailedTemplate, ContextName);
        }
    }
}
