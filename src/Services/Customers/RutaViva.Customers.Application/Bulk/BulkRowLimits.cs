namespace RutaViva.Customers.Application.Bulk;

internal static class BulkRowLimits
{
    public const int MaxRows = 500;
    public const string TooManyRowsMessage = "The batch exceeds the maximum number of rows.";
}
