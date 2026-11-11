namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public static class Eventually
{
    private const int PollMilliseconds = 100;

    public static async Task<bool> UntilAsync(Func<Task<bool>> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        long started = TimeProvider.System.GetTimestamp();
        while (TimeProvider.System.GetElapsedTime(started) < timeout)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(PollMilliseconds), cancellationToken);
        }

        return await condition();
    }
}
