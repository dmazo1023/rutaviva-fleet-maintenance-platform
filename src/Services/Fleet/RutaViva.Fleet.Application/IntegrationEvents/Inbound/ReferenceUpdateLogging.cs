using Microsoft.Extensions.Logging;

namespace RutaViva.Fleet.Application.IntegrationEvents.Inbound;

internal static class ReferenceUpdateLogging
{
    private const string IgnoredTemplate = "ReferenceUpdateIgnored: {ReferenceType} {ReferenceId} is unknown, requested status {Status}";

    public static void LogReferenceUpdateIgnored(this ILogger logger, string referenceType, Guid referenceId, string status) =>
        logger.LogWarning(IgnoredTemplate, referenceType, referenceId, status);
}
