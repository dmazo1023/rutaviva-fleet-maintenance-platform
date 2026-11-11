using System.Text.Json;
using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.Validation;

public static partial class EnvelopeValidator
{
    private const string TypePattern = @"^rutaviva\.[a-z][a-z0-9-]*\.[A-Z][A-Za-z0-9]*\.v[1-9][0-9]*$";

    public static Result Validate(IntegrationEventEnvelope? envelope)
    {
        if (envelope is null)
        {
            return Result.Failure(EventBusErrors.EnvelopeMissing);
        }

        Error? error = FindError(envelope);
        return error is null ? Result.Success() : Result.Failure(error);
    }

    private static Error? FindError(IntegrationEventEnvelope envelope)
    {
        if (envelope.Id == Guid.Empty)
        {
            return EventBusErrors.EnvelopeIdMissing;
        }

        if (string.IsNullOrWhiteSpace(envelope.Source))
        {
            return EventBusErrors.EnvelopeSourceMissing;
        }

        return FindTypeError(envelope.Type) ?? FindRemainingError(envelope);
    }

    private static Error? FindTypeError(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return EventBusErrors.EnvelopeTypeMissing;
        }

        return TypeRegex().IsMatch(type) ? null : EventBusErrors.EnvelopeTypeInvalid;
    }

    private static Error? FindRemainingError(IntegrationEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.Subject))
        {
            return EventBusErrors.EnvelopeSubjectMissing;
        }

        if (envelope.Time == default)
        {
            return EventBusErrors.EnvelopeTimeMissing;
        }

        return envelope.Data.ValueKind == JsonValueKind.Undefined ? EventBusErrors.EnvelopeDataMissing : null;
    }

    [GeneratedRegex(TypePattern, RegexOptions.CultureInvariant)]
    private static partial Regex TypeRegex();
}
