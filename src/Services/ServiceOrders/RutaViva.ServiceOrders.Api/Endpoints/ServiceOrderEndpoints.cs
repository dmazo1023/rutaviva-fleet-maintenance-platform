using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Api.Contracts;
using RutaViva.ServiceOrders.Application.Orders;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Api.Endpoints;

public sealed class ServiceOrderEndpoints : IEndpointModule
{
    private const string OrdersRoute = "/api/v1/service-orders";
    private const string OrderRoute = OrdersRoute + "/{id}";
    private const string QuoteRoute = OrderRoute + "/quote";
    private const string ApproveRoute = OrderRoute + "/approve";
    private const string RejectRoute = OrderRoute + "/reject";
    private const string StartRoute = OrderRoute + "/start";
    private const string CompleteRoute = OrderRoute + "/complete";
    private const string CancelRoute = OrderRoute + "/cancel";
    private const string LocationFormat = OrdersRoute + "/{0}";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(OrdersRoute, CreateAsync);
        app.MapGet(OrdersRoute, ListAsync);
        app.MapGet(OrderRoute, GetDetailAsync);
        app.MapPost(QuoteRoute, QuoteAsync);
        app.MapPost(ApproveRoute, ApproveAsync);
        app.MapPost(RejectRoute, RejectAsync);
        app.MapPost(StartRoute, StartAsync);
        app.MapPost(CompleteRoute, CompleteAsync);
        app.MapPost(CancelRoute, CancelAsync);
    }

    private static async Task<IResult> CreateAsync(
        CreateServiceOrderRequest request,
        ICommandHandler<CreateServiceOrderCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken)
    {
        CreateServiceOrderCommand command = new(request.VehicleId, request.RoutineId, ServiceOrderOrigin.Manual, request.CreatedBy);
        Result<ServiceOrderId> created = await handler.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
        {
            return created.ToHttpResult();
        }

        Result<ServiceOrderDetailDto> order = await detail.HandleAsync(new GetServiceOrderDetailQuery(created.Value, ServiceOrderActor.Operator), cancellationToken);
        if (order.IsFailure)
        {
            return order.ToHttpResult();
        }

        return TypedResults.Created(string.Format(LocationFormat, created.Value), order.Value);
    }

    private static async Task<IResult> ListAsync(
        Guid? customerId,
        Guid? vehicleId,
        Guid? routineId,
        Guid? workshopId,
        ServiceOrderStatusGroup? statusGroup,
        IQueryHandler<ListServiceOrdersQuery, IReadOnlyList<ServiceOrderListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ServiceOrderListItemDto>> result =
            await handler.HandleAsync(new ListServiceOrdersQuery(customerId, vehicleId, routineId, workshopId, statusGroup), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        ServiceOrderActor? actor,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> handler,
        CancellationToken cancellationToken)
    {
        Result<ServiceOrderId> order = ServiceOrderId.Create(id);
        if (order.IsFailure)
        {
            return order.ToHttpResult();
        }

        Result<ServiceOrderDetailDto> result = await handler.HandleAsync(new GetServiceOrderDetailQuery(order.Value, actor), cancellationToken);
        return result.ToHttpResult();
    }

    private static Task<IResult> QuoteAsync(
        Guid id,
        QuoteServiceOrderRequest request,
        ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken) =>
        ExecuteAsync(id, new ActionRequest(ServiceOrderAction.Quote, request.QuotedBy, request.Amount, null, null, request.WorkshopId), ServiceOrderActor.Workshop, handler, detail, cancellationToken);

    private static Task<IResult> ApproveAsync(
        Guid id,
        ApproveServiceOrderRequest request,
        ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken) =>
        ExecuteAsync(id, new ActionRequest(ServiceOrderAction.Approve, request.ApprovedBy, null, null, request.ApproverId, null), ServiceOrderActor.Approver, handler, detail, cancellationToken);

    private static Task<IResult> RejectAsync(
        Guid id,
        RejectServiceOrderRequest request,
        ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken) =>
        ExecuteAsync(id, new ActionRequest(ServiceOrderAction.Reject, request.RejectedBy, null, null, request.ApproverId, null), ServiceOrderActor.Approver, handler, detail, cancellationToken);

    private static Task<IResult> StartAsync(
        Guid id,
        StartServiceOrderRequest request,
        ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken) =>
        ExecuteAsync(id, new ActionRequest(ServiceOrderAction.Start, request.StartedBy, null, null, null, request.WorkshopId), ServiceOrderActor.Workshop, handler, detail, cancellationToken);

    private static Task<IResult> CompleteAsync(
        Guid id,
        CompleteServiceOrderRequest request,
        ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken) =>
        ExecuteAsync(id, new ActionRequest(ServiceOrderAction.Complete, request.CompletedBy, null, request.ServicedKm, null, request.WorkshopId), ServiceOrderActor.Workshop, handler, detail, cancellationToken);

    private static Task<IResult> CancelAsync(
        Guid id,
        CancelServiceOrderRequest request,
        ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken) =>
        ExecuteAsync(id, new ActionRequest(ServiceOrderAction.Cancel, request.CancelledBy, null, null, null, null), ServiceOrderActor.Operator, handler, detail, cancellationToken);

    private static async Task<IResult> ExecuteAsync(
        Guid id,
        ActionRequest request,
        ServiceOrderActor actor,
        ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId> handler,
        IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto> detail,
        CancellationToken cancellationToken)
    {
        Result<ServiceOrderId> order = ServiceOrderId.Create(id);
        if (order.IsFailure)
        {
            return order.ToHttpResult();
        }

        ExecuteServiceOrderActionCommand command = new(order.Value, request.Action, request.Actor, request.Amount, request.ServicedKm, request.ApproverId, request.WorkshopId);
        Result<ServiceOrderId> executed = await handler.HandleAsync(command, cancellationToken);
        if (executed.IsFailure)
        {
            return executed.ToHttpResult();
        }

        Result<ServiceOrderDetailDto> result = await detail.HandleAsync(new GetServiceOrderDetailQuery(order.Value, actor), cancellationToken);
        return result.ToHttpResult();
    }

    private sealed record ActionRequest(ServiceOrderAction Action, string? Actor, decimal? Amount, long? ServicedKm, Guid? ApproverId, Guid? WorkshopId);
}
