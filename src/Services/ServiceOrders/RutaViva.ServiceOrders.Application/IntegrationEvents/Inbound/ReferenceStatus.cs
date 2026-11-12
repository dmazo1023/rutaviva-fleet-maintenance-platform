namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

internal static class ReferenceStatus
{
    private const string Active = "Active";

    public static bool IsActive(string status) => string.Equals(status, Active, StringComparison.Ordinal);
}
