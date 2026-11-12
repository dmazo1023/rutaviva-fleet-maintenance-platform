namespace RutaViva.ServiceOrders.Domain;

public sealed record ServiceOrderApprover(Guid Id, Guid CustomerId, string FullName, bool IsActive);
