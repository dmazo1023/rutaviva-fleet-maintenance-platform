using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.Fleet.Infrastructure.Persistence.Configurations;
using RutaViva.Fleet.Infrastructure.Persistence.Converters;

namespace RutaViva.Fleet.Infrastructure.Persistence;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<CustomerReference> CustomerReferences => Set<CustomerReference>();

    public DbSet<WorkshopReference> WorkshopReferences => Set<WorkshopReference>();

    public DbSet<ApproverReference> ApproverReferences => Set<ApproverReference>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<VehicleId>().HaveConversion<VehicleIdConverter>();
        configurationBuilder.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
        configurationBuilder.Properties<WorkshopId>().HaveConversion<WorkshopIdConverter>();
        configurationBuilder.Properties<ApproverId>().HaveConversion<ApproverIdConverter>();
        configurationBuilder.Properties<Plate>().HaveConversion<PlateConverter>();
        configurationBuilder.Properties<Vin>().HaveConversion<VinConverter>();
        configurationBuilder.Properties<Kilometers>().HaveConversion<KilometersConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(FleetSchema.Name);
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new WorkshopReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new ApproverReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(FleetSchema.Name));
        modelBuilder.ApplyConfiguration(new EventCheckpointConfiguration(FleetSchema.Name));
        modelBuilder.ApplyRutaVivaConventions();
        modelBuilder.ApplyEnumCheckConstraints();
    }
}
