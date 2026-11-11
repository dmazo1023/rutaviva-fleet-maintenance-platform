using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Maintenance.Application.Routines.Bulk;

public static class BulkDuplicateDetector
{
    private const char KeySeparator = '|';

    public static IReadOnlySet<int> FindDuplicateIndexes(IReadOnlyList<RoutineBulkRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        HashSet<string> seenKeys = new(StringComparer.Ordinal);
        HashSet<int> duplicates = [];
        for (int index = 0; index < rows.Count; index++)
        {
            if (!seenKeys.Add(KeyOf(rows[index])))
            {
                duplicates.Add(index);
            }
        }

        return duplicates;
    }

    private static string KeyOf(RoutineBulkRow row) =>
        string.Concat(Normalize(row.Plate), KeySeparator, Normalize(row.RoutineTypeCode));

    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return string.Concat(value.Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
    }
}
