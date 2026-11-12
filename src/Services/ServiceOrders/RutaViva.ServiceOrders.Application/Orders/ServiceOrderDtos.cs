using RutaViva.ServiceOrders.Domain;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed record ServiceOrderActionDto(ServiceOrderAction Action, bool Enabled, string? BlockedReason);

public sealed record ServiceLineDto(
    string Id,
    string LineType,
    string Description,
    decimal? Amount,
    string PricingStatus,
    string RequestedBy,
    string ApprovalStatus);

public sealed record ServiceOrderHistoryEntryDto(
    Guid Id,
    string ChangeType,
    string Detail,
    string ChangedBy,
    DateTimeOffset ChangedAt,
    string Icon);

public sealed record ServiceOrderListItemDto(
    Guid Id,
    string Number,
    Guid VehicleId,
    string Plate,
    Guid RoutineId,
    string RoutineName,
    string RoutineTypeCode,
    Guid CustomerId,
    ServiceOrderStatus Status,
    ServiceOrderStatus? StatusBeforeHold,
    string? Reason,
    string? Priority,
    decimal? Total,
    DateTimeOffset? AppointmentAt,
    DateTimeOffset UpdatedAt);

public sealed record ServiceOrderDetailDto(
    Guid Id,
    string Number,
    Guid VehicleId,
    string Plate,
    Guid RoutineId,
    string RoutineName,
    string RoutineTypeCode,
    string RoutineIcon,
    Guid CustomerId,
    Guid WorkshopId,
    string WorkshopName,
    ServiceOrderStatus Status,
    ServiceOrderStatus? StatusBeforeHold,
    string? Reason,
    string? Priority,
    IReadOnlyList<ServiceOrderActionDto> AvailableActions,
    IReadOnlyList<ServiceLineDto> Lines,
    decimal Total,
    bool AutoApproved,
    decimal AutoApprovalMaxAmountApplied,
    DateTimeOffset? AppointmentAt,
    IReadOnlyList<ServiceOrderHistoryEntryDto> History,
    DateTimeOffset UpdatedAt);
