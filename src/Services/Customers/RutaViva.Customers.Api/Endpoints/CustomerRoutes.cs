namespace RutaViva.Customers.Api.Endpoints;

internal static class CustomerRoutes
{
    public const string Customers = "/api/v1/customers";
    public const string Customer = Customers + "/{id}";
    public const string Approvers = Customer + "/approvers";
    public const string CustomersBulk = Customers + "/bulk";
    public const string ApproversBulk = Customers + "/approvers/bulk";
    public const string LocationFormat = Customers + "/{0}";
}
