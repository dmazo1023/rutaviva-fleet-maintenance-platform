using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Api.Contracts;
using RutaViva.Maintenance.Application.Telemetry;

namespace RutaViva.Maintenance.Api.Endpoints;

public sealed class TelemetryEndpoints : IEndpointModule
{
    private const string OdometerReadingsRoute = "/api/v1/maintenance/odometer-readings";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(OdometerReadingsRoute, IngestAsync);
    }

    private static async Task<IResult> IngestAsync(
        TelemetryOdometerReadingRequest request,
        ICommandHandler<IngestTelemetryReadingCommand, TelemetryReadingOutcome> handler,
        CancellationToken cancellationToken)
    {
        IngestTelemetryReadingCommand command = new(request.Plate ?? string.Empty, request.OdometerKm, request.ReadAt);
        Result<TelemetryReadingOutcome> result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        TelemetryOdometerReadingResponse response = new(result.Value.Outcome, result.Value.IgnoredReason);
        return TypedResults.Accepted(uri: (string?)null, value: response);
    }
}
