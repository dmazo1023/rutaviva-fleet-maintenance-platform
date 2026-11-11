using Polly;
using Polly.Retry;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal static class SubscriberRetryPipeline
{
    private const int RetryAttempts = 3;
    private const int BaseDelaySeconds = 1;

    public static ResiliencePipeline<Result> Create(TimeProvider timeProvider)
        => new ResiliencePipelineBuilder<Result> { TimeProvider = timeProvider }
            .AddRetry(new RetryStrategyOptions<Result>
            {
                MaxRetryAttempts = RetryAttempts,
                Delay = TimeSpan.FromSeconds(BaseDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder<Result>().HandleResult(result => result.IsFailure),
            })
            .Build();
}
