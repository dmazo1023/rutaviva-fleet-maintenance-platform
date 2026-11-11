using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;

namespace RutaViva.EventBus.Application.Publishing;

public sealed class PublishEventCommandHandler(IEventStore store) : ICommandHandler<PublishEventCommand, long>
{
    public async Task<Result<long>> HandleAsync(PublishEventCommand command, CancellationToken cancellationToken)
    {
        long offset = await store.AppendAsync(command.Stream, command.Envelope, cancellationToken);
        return offset;
    }
}
