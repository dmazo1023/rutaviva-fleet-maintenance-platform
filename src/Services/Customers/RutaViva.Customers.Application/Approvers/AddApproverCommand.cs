using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Approvers;

public sealed record AddApproverCommand(CustomerId CustomerId, string FullName, string Email) : ICommand<Guid>;
