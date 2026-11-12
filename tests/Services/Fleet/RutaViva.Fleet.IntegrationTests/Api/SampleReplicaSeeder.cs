using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Infrastructure.Persistence;
using RutaViva.Fleet.IntegrationTests.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Api;

internal static class SampleReplicaSeeder
{
    private const int TaxIdColumn = 0;
    private const int NameColumn = 1;
    private const int ApproverCustomerColumn = 0;
    private const int ApproverNameColumn = 1;
    private const int ApproverEmailColumn = 2;

    public static async Task SeedAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        Dictionary<string, CustomerReference> customers = SeedCustomers(context);
        SeedWorkshops(context);
        SeedApprovers(context, customers);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, CustomerReference> SeedCustomers(FleetDbContext context)
    {
        Dictionary<string, CustomerReference> customers = [];
        foreach (string[] fields in SampleImportFiles.ReadRows(SampleImportFiles.CustomersFile))
        {
            CustomerReference customer = CustomerReference.Create(
                CustomerId.New(),
                fields[TaxIdColumn],
                fields[NameColumn],
                ReferenceStatus.Active,
                FleetTestData.Instant());
            customers[customer.TaxId] = customer;
            context.CustomerReferences.Add(customer);
        }

        return customers;
    }

    private static void SeedWorkshops(FleetDbContext context)
    {
        foreach (string[] fields in SampleImportFiles.ReadRows(SampleImportFiles.WorkshopsFile))
        {
            context.WorkshopReferences.Add(WorkshopReference.Create(
                WorkshopId.New(),
                fields[TaxIdColumn],
                fields[NameColumn],
                ReferenceStatus.Active,
                FleetTestData.Instant()));
        }
    }

    private static void SeedApprovers(FleetDbContext context, Dictionary<string, CustomerReference> customers)
    {
        foreach (string[] fields in SampleImportFiles.ReadRows(SampleImportFiles.ApproversFile))
        {
            context.ApproverReferences.Add(ApproverReference.Create(
                ApproverId.New(),
                customers[fields[ApproverCustomerColumn]].Id,
                fields[ApproverNameColumn],
                fields[ApproverEmailColumn],
                ReferenceStatus.Active,
                FleetTestData.Instant()));
        }
    }
}
