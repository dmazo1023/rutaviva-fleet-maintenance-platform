using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;

namespace RutaViva.EventBus.Application.Failures;

public sealed class ReportFailureCommandHandler(IEventStore store) : ICommandHandler<ReportFailureCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(ReportFailureCommand command, CancellationToken cancellationToken)
    {
        await store.AddDeadLetterAsync(command.Subscriber, command.Offset, command.Error, cancellationToken);
        return true;
    }
}
