using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Workshops.Api.Contracts;
using RutaViva.Workshops.Application.Bulk;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Api.Endpoints;

public sealed class WorkshopEndpoints : IEndpointModule
{
    private const string WorkshopsRoute = "/api/v1/workshops";
    private const string WorkshopRoute = WorkshopsRoute + "/{id}";
    private const string WorkshopStatusRoute = WorkshopRoute + "/status";
    private const string BulkRoute = WorkshopsRoute + "/bulk";
    private const string LocationFormat = WorkshopsRoute + "/{0}";
    private const int MaxBulkRows = 500;
    private const string TooManyRowsCode = "Bulk.TooManyRows";
    private const string TooManyRowsMessage = "The batch exceeds the maximum number of rows.";
    private const string StatusRequiredCode = "Workshop.StatusRequired";
    private const string StatusRequiredMessage = "The workshop status is required.";

    private static readonly Error TooManyRows = new(TooManyRowsCode, TooManyRowsMessage, ErrorType.Validation);
    private static readonly Error StatusRequired = new(StatusRequiredCode, StatusRequiredMessage, ErrorType.Validation);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(WorkshopsRoute, RegisterAsync);
        app.MapGet(WorkshopsRoute, ListAsync);
        app.MapGet(WorkshopRoute, GetDetailAsync);
        app.MapPatch(WorkshopStatusRoute, ChangeStatusAsync);
        app.MapPost(BulkRoute, RegisterBulkAsync);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterWorkshopRequest request,
        ICommandHandler<RegisterWorkshopCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        RegisterWorkshopCommand command = new(
            request.Name,
            request.TaxId,
            request.Email,
            request.Phone,
            request.Address,
            request.City);
        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        return TypedResults.Created(string.Format(LocationFormat, result.Value), result.Value);
    }

    private static async Task<IResult> ListAsync(
        WorkshopStatus? status,
        string? city,
        IQueryHandler<ListWorkshopsQuery, IReadOnlyList<WorkshopSummaryDto>> handler,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<WorkshopSummaryDto>> result = await handler.HandleAsync(new ListWorkshopsQuery(status, city), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        IQueryHandler<GetWorkshopDetailQuery, WorkshopDetailDto> handler,
        CancellationToken cancellationToken)
    {
        Result<WorkshopId> workshop = WorkshopId.Create(id);
        if (workshop.IsFailure)
        {
            return workshop.ToHttpResult();
        }

        Result<WorkshopDetailDto> result = await handler.HandleAsync(new GetWorkshopDetailQuery(workshop.Value), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeWorkshopStatusRequest request,
        ICommandHandler<ChangeWorkshopStatusCommand, WorkshopId> handler,
        CancellationToken cancellationToken)
    {
        if (request.Status is not { } status)
        {
            return Result.Failure(StatusRequired).ToHttpResult();
        }

        Result<WorkshopId> workshop = WorkshopId.Create(id);
        if (workshop.IsFailure)
        {
            return workshop.ToHttpResult();
        }

        Result<WorkshopId> result = await handler.HandleAsync(new ChangeWorkshopStatusCommand(workshop.Value, status), cancellationToken);
        return ToNoContent(result);
    }

    private static async Task<IResult> RegisterBulkAsync(
        BulkRequest<WorkshopBulkRow> request,
        WorkshopBulkRegistrationHandlerFactory factory,
        CancellationToken cancellationToken,
        bool dryRun = false)
    {
        IReadOnlyList<WorkshopBulkRow> rows = request.Rows ?? [];
        if (rows.Count > MaxBulkRows)
        {
            return Result.Failure(TooManyRows).ToHttpResult();
        }

        IReadOnlyList<BulkRowResult> results = await factory.Create(dryRun).ExecuteAsync(rows, cancellationToken);
        return TypedResults.Ok(results);
    }

    private static IResult ToNoContent<T>(Result<T> result)
    {
        Result outcome = result;
        return outcome.ToHttpResult();
    }
}
