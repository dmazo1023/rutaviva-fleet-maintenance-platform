namespace RutaViva.ServiceOrders.Domain;

public sealed class ServiceOrderHistoryEntry
{
    private ServiceOrderHistoryEntry()
    {
        ChangedBy = null!;
    }

    internal ServiceOrderHistoryEntry(ServiceOrderStatus status, string changedBy, DateTimeOffset changedAt)
    {
        Id = Guid.CreateVersion7();
        Status = status;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private set; }

    public ServiceOrderStatus Status { get; private set; }

    public string ChangedBy { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }
}
