namespace RutaViva.BuildingBlocks.Application.Abstractions;

public interface IEventCheckpointStore
{
    Task<long> GetLastOffsetAsync(string subscriberName, CancellationToken cancellationToken);

    Task SaveAsync(string subscriberName, long offset, CancellationToken cancellationToken);
}
