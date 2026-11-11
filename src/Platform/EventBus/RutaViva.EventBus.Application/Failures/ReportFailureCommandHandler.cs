using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;

namespace RutaViva.EventBus.Application.Failures;

public sealed class ReportFailureCommandHandler(IEventStore store) : ICommandHandler<ReportFailureCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(ReportFailureCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Subscriber))
        {
            return EventBusErrors.SubscriberMissing;
        }

        if (command.Offset < 0)
        {
            return EventBusErrors.OffsetInvalid;
        }

        if (string.IsNullOrWhiteSpace(command.Error))
        {
            return EventBusErrors.FailureErrorMissing;
        }

        string error = Truncate(command.Error);
        await store.AddDeadLetterAsync(command.Subscriber, command.Offset, error, cancellationToken);
        return true;
    }

    private static string Truncate(string error) =>
        error.Length <= ReportFailureCommand.MaxErrorLength ? error : error[..ReportFailureCommand.MaxErrorLength];
}
