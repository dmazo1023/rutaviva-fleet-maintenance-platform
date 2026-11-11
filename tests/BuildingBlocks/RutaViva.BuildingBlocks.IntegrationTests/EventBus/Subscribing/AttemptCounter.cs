namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class AttemptCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment() => Interlocked.Increment(ref _count);
}
