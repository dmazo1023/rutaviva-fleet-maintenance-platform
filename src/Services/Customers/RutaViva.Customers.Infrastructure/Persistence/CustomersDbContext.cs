using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;
using RutaViva.Customers.Infrastructure.Persistence.Configurations;
using RutaViva.Customers.Infrastructure.Persistence.Converters;

namespace RutaViva.Customers.Infrastructure.Persistence;

public sealed class CustomersDbContext(DbContextOptions<CustomersDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Approver> Approvers => Set<Approver>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
        configurationBuilder.Properties<ApproverId>().HaveConversion<ApproverIdConverter>();
        configurationBuilder.Properties<TaxId>().HaveConversion<TaxIdConverter>();
        configurationBuilder.Properties<PersonName>().HaveConversion<PersonNameConverter>();
        configurationBuilder.Properties<EmailAddress>().HaveConversion<EmailAddressConverter>();
        configurationBuilder.Properties<PhoneNumber>().HaveConversion<PhoneNumberConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(CustomersSchema.Name);
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ApproverConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(CustomersSchema.Name));
        modelBuilder.ApplyConfiguration(new EventCheckpointConfiguration(CustomersSchema.Name));
        modelBuilder.ApplyRutaVivaConventions();
        modelBuilder.ApplyEnumCheckConstraints();
    }
}
