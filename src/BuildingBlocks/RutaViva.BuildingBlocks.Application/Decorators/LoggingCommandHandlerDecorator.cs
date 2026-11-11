using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Application.Decorators;

public sealed class LoggingCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<LoggingCommandHandlerDecorator<TCommand, TResult>> logger) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private const string SucceededTemplate = "Command {CommandName} succeeded";
    private const string FailedTemplate = "Command {CommandName} failed with {ErrorCode} ({ErrorType})";

    private static readonly string CommandName = typeof(TCommand).Name;

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        Result<TResult> result = await inner.HandleAsync(command, cancellationToken);
        LogOutcome(result);
        return result;
    }

    private void LogOutcome(Result<TResult> result)
    {
        if (result.IsSuccess)
        {
            logger.LogInformation(SucceededTemplate, CommandName);
            return;
        }

        logger.LogWarning(FailedTemplate, CommandName, result.Error.Code, result.Error.Type);
    }
}
