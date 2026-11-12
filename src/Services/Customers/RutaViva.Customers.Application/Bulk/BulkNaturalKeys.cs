namespace RutaViva.Customers.Application.Bulk;

internal static class BulkNaturalKeys
{
    private const char Separator = '|';
    private const string RowKeyPrefix = "#";
    private const int SeparatedParts = 2;
    private const int TaxIdPart = 0;
    private const int EmailPart = 1;

    public static string ForTaxId(string? taxId, int rowNumber)
    {
        string normalized = NormalizeTaxId(taxId);
        return normalized.Length == 0 ? RowKey(rowNumber) : normalized;
    }

    public static string ForApprover(string? taxId, string? email, int rowNumber)
    {
        string normalizedTaxId = NormalizeTaxId(taxId);
        string normalizedEmail = NormalizeEmail(email);
        if (normalizedTaxId.Length == 0 || normalizedEmail.Length == 0)
        {
            return RowKey(rowNumber);
        }

        return string.Concat(normalizedTaxId, Separator, normalizedEmail);
    }

    public static bool TryParseApprover(string naturalKey, out string taxId, out string email)
    {
        string[] parts = naturalKey.Split(Separator, SeparatedParts);
        taxId = string.Empty;
        email = string.Empty;
        if (parts.Length != SeparatedParts)
        {
            return false;
        }

        taxId = parts[TaxIdPart];
        email = parts[EmailPart];
        return true;
    }

    private static string RowKey(int rowNumber) => string.Concat(RowKeyPrefix, rowNumber);

    private static string NormalizeTaxId(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

    private static string NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
