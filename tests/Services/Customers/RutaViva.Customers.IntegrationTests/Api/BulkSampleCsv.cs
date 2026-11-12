using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.IntegrationTests.Api;

internal static class BulkSampleCsv
{
    private const char Separator = ';';
    private const string CustomersFile = "clientes.csv";
    private const string ApproversFile = "aprobadores.csv";
    private const int HeaderRows = 1;
    private const int FirstRowNumber = 1;
    private const int CustomerTaxIdColumn = 0;
    private const int CustomerNameColumn = 1;
    private const int CustomerEmailColumn = 2;
    private const int CustomerPhoneColumn = 3;
    private const int ApproverTaxIdColumn = 0;
    private const int ApproverNameColumn = 1;
    private const int ApproverEmailColumn = 2;

    public static IReadOnlyList<CustomerBulkRow> Customers() =>
        ReadRows(CustomersFile).Select(ToCustomer).ToList();

    public static IReadOnlyList<ApproverBulkRow> Approvers() =>
        ReadRows(ApproversFile).Select(ToApprover).ToList();

    private static IEnumerable<string[]> ReadRows(string fileName) =>
        File.ReadAllLines(SampleDataLocator.ImportFile(fileName))
            .Skip(HeaderRows)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(Separator));

    private static CustomerBulkRow ToCustomer(string[] fields, int index) =>
        new(
            index + FirstRowNumber,
            fields[CustomerTaxIdColumn],
            fields[CustomerNameColumn],
            fields[CustomerEmailColumn],
            fields[CustomerPhoneColumn]);

    private static ApproverBulkRow ToApprover(string[] fields, int index) =>
        new(
            index + FirstRowNumber,
            fields[ApproverTaxIdColumn],
            fields[ApproverNameColumn],
            fields[ApproverEmailColumn]);
}
