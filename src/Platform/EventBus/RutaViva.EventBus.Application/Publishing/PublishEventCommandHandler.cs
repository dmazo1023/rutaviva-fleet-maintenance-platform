using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Application.Validation;

namespace RutaViva.EventBus.Application.Publishing;

public sealed class PublishEventCommandHandler(IEventStore store) : ICommandHandler<PublishEventCommand, long>
{
    public async Task<Result<long>> HandleAsync(PublishEventCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Stream))
        {
            return EventBusErrors.StreamMissing;
        }

        Result validation = EnvelopeValidator.Validate(command.Envelope);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        long offset = await store.AppendAsync(command.Stream, command.Envelope, cancellationToken);
        return offset;
    }
}
