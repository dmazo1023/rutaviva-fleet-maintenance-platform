using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.ValueObjects;
using RutaViva.Workshops.Infrastructure.Persistence.Configurations;
using RutaViva.Workshops.Infrastructure.Persistence.Converters;

namespace RutaViva.Workshops.Infrastructure.Persistence;

public sealed class WorkshopsDbContext(DbContextOptions<WorkshopsDbContext> options) : DbContext(options)
{
    public DbSet<Workshop> Workshops => Set<Workshop>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<WorkshopId>().HaveConversion<WorkshopIdConverter>();
        configurationBuilder.Properties<TaxId>().HaveConversion<TaxIdConverter>();
        configurationBuilder.Properties<EmailAddress>().HaveConversion<EmailAddressConverter>();
        configurationBuilder.Properties<PhoneNumber>().HaveConversion<PhoneNumberConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(WorkshopsSchema.Name);
        modelBuilder.ApplyConfiguration(new WorkshopConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(WorkshopsSchema.Name));
        modelBuilder.ApplyConfiguration(new EventCheckpointConfiguration(WorkshopsSchema.Name));
        modelBuilder.ApplyRutaVivaConventions();
        modelBuilder.ApplyEnumCheckConstraints();
    }
}
