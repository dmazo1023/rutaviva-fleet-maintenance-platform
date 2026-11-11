using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Approvers;

public sealed record ChangeApproverStatusCommand(CustomerId CustomerId, ApproverId ApproverId, ApproverStatus TargetStatus)
    : ICommand<ApproverId>;
