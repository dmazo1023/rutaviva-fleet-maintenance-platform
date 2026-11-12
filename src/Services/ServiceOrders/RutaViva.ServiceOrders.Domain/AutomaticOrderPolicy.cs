namespace RutaViva.ServiceOrders.Domain;

public sealed record AutomaticOrderPolicy(DateTimeOffset EnabledSince)
{
    public bool Applies(DateTimeOffset overdueAt) => overdueAt >= EnabledSince;
}
