using System.Globalization;
using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Domain.Common;
using RutaViva.ServiceOrders.Domain.Events;

namespace RutaViva.ServiceOrders.Domain;

public sealed class ServiceOrder : AggregateRoot<ServiceOrderId>
{
    public const int MaxTextLength = 200;
    public const int MaxNumberLength = 20;
    public const int MaxPlateLength = 10;
    public const int MaxRoutineTypeCodeLength = 50;

    private const string NumberFormat = "OS-{0}";
    private const string GuidDigitsFormat = "N";
    private const int NumberSuffixLength = 8;

    private readonly List<ServiceOrderHistoryEntry> _history = [];

    private ServiceOrder()
    {
        Number = null!;
        Reference = null!;
    }

    public string Number { get; private set; }

    public ServiceOrderReference Reference { get; private set; }

    public ServiceOrderOrigin Origin { get; private set; }

    public ServiceOrderStatus Status { get; private set; }

    public decimal? QuotedAmount { get; private set; }

    public bool AutoApproved { get; private set; }

    public decimal AutoApprovalMaxAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ServiceOrderHistoryEntry> History => _history.AsReadOnly();

    public static Result<ServiceOrder> Create(ServiceOrderReference reference, ServiceOrderOrigin origin, string? createdBy)
    {
        Guard.NotNull(reference);
        Result validReference = Validate(reference);
        if (validReference.IsFailure)
        {
            return validReference.Error;
        }

        Result<string> actor = ValidateActor(createdBy);
        if (actor.IsFailure)
        {
            return actor.Error;
        }

        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        ServiceOrderId id = ServiceOrderId.New();
        ServiceOrder order = new()
        {
            Id = id,
            Number = NumberOf(id),
            Reference = reference,
            Origin = origin,
            Status = ServiceOrderStatus.Created,
            CreatedAt = now,
            UpdatedAt = now
        };
        order._history.Add(new ServiceOrderHistoryEntry(ServiceOrderStatus.Created, actor.Value, now));
        return order;
    }

    public Result Quote(decimal amount, QuoteApprovalPolicy policy, Guid workshopId, string? quotedBy)
    {
        Guard.NotNull(policy);
        if (!IsAssignedWorkshop(workshopId))
        {
            return Result.Failure(ServiceOrderErrors.WorkshopNotAuthorized);
        }

        if (amount <= 0)
        {
            return Result.Failure(ServiceOrderErrors.AmountInvalid);
        }

        Result quoted = MoveTo(ServiceOrderStatus.Quoted, quotedBy);
        if (quoted.IsFailure)
        {
            return quoted;
        }

        QuotedAmount = amount;
        AutoApprovalMaxAmount = policy.AutoApprovalMaxAmount;
        AutoApproved = !policy.RequiresApproval(amount);
        ServiceOrderStatus next = AutoApproved ? ServiceOrderStatus.Approved : ServiceOrderStatus.PendingApproval;
        return MoveTo(next, quotedBy);
    }

    public Result Approve(ServiceOrderApprover approver) => Decide(approver, ServiceOrderStatus.Approved);

    public Result Reject(ServiceOrderApprover approver) => Decide(approver, ServiceOrderStatus.Cancelled);

    public Result Start(Guid workshopId, string? startedBy) =>
        IsAssignedWorkshop(workshopId)
            ? MoveTo(ServiceOrderStatus.InProgress, startedBy)
            : Result.Failure(ServiceOrderErrors.WorkshopNotAuthorized);

    public Result Cancel(string? cancelledBy) => MoveTo(ServiceOrderStatus.Cancelled, cancelledBy);

    public Result Complete(long servicedKm, Guid workshopId, string? completedBy)
    {
        if (!IsAssignedWorkshop(workshopId))
        {
            return Result.Failure(ServiceOrderErrors.WorkshopNotAuthorized);
        }

        if (servicedKm < 0)
        {
            return Result.Failure(ServiceOrderErrors.ServicedKmInvalid);
        }

        Result completed = MoveTo(ServiceOrderStatus.Completed, completedBy);
        if (completed.IsFailure)
        {
            return completed;
        }

        Raise(new ServiceOrderCompleted(Id, Number, Reference, Origin, QuotedAmount ?? 0, AutoApproved, servicedKm, CreatedAt, UpdatedAt));
        return Result.Success();
    }

    private static string NumberOf(ServiceOrderId id)
    {
        string digits = id.Value.ToString(GuidDigitsFormat, CultureInfo.InvariantCulture).ToUpperInvariant();
        return string.Format(CultureInfo.InvariantCulture, NumberFormat, digits[^NumberSuffixLength..]);
    }

    private static Result Validate(ServiceOrderReference reference)
    {
        Guid[] identifiers = [reference.VehicleId, reference.CustomerId, reference.WorkshopId, reference.RoutineId];
        if (identifiers.Contains(Guid.Empty))
        {
            return Result.Failure(ServiceOrderErrors.ReferenceRequired);
        }

        string[] texts = [reference.Plate, reference.WorkshopName, reference.RoutineName, reference.RoutineTypeCode];
        if (texts.Any(string.IsNullOrWhiteSpace))
        {
            return Result.Failure(ServiceOrderErrors.TextRequired);
        }

        return texts.Any(text => text.Length > MaxTextLength) || reference.Plate.Length > MaxPlateLength
            ? Result.Failure(ServiceOrderErrors.TextTooLong)
            : Result.Success();
    }

    private static Result<string> ValidateActor(string? actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            return ServiceOrderErrors.TextRequired;
        }

        string normalized = actor.Trim();
        if (normalized.Length > MaxTextLength)
        {
            return ServiceOrderErrors.TextTooLong;
        }

        return normalized;
    }

    private bool IsAssignedWorkshop(Guid workshopId) => workshopId == Reference.WorkshopId;

    private Result Decide(ServiceOrderApprover approver, ServiceOrderStatus target)
    {
        Guard.NotNull(approver);
        if (!ApprovalAuthorizationPolicy.CanApprove(Reference, approver))
        {
            return Result.Failure(ServiceOrderErrors.ApproverNotAuthorized);
        }

        return MoveFrom(ServiceOrderStatus.PendingApproval, target, approver.FullName);
    }

    private Result MoveFrom(ServiceOrderStatus expected, ServiceOrderStatus target, string? actor) =>
        Status == expected ? MoveTo(target, actor) : Result.Failure(ServiceOrderErrors.InvalidTransition);

    private Result MoveTo(ServiceOrderStatus target, string? actor)
    {
        if (!ServiceOrderTransitions.CanMove(Status, target))
        {
            return Result.Failure(ServiceOrderErrors.InvalidTransition);
        }

        Result<string> validActor = ValidateActor(actor);
        if (validActor.IsFailure)
        {
            return Result.Failure(validActor.Error);
        }

        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        Status = target;
        UpdatedAt = now;
        _history.Add(new ServiceOrderHistoryEntry(target, validActor.Value, now));
        return Result.Success();
    }
}
