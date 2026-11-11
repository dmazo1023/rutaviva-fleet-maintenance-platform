namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class ImmediateTimeProvider : TimeProvider
{
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        => base.CreateTimer(callback, state, TimeSpan.Zero, period);
}
