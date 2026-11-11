using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;
using RutaViva.Maintenance.Infrastructure.Persistence.Configurations;
using RutaViva.Maintenance.Infrastructure.Persistence.Converters;

namespace RutaViva.Maintenance.Infrastructure.Persistence;

public sealed class MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options) : DbContext(options)
{
    public DbSet<RoutineType> RoutineTypes => Set<RoutineType>();

    public DbSet<MaintenancePlan> MaintenancePlans => Set<MaintenancePlan>();

    public DbSet<MaintenanceRoutine> MaintenanceRoutines => Set<MaintenanceRoutine>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<OdometerReadingRecord> OdometerReadings => Set<OdometerReadingRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<VehicleId>().HaveConversion<VehicleIdConverter>();
        configurationBuilder.Properties<RoutineId>().HaveConversion<RoutineIdConverter>();
        configurationBuilder.Properties<RoutineTypeId>().HaveConversion<RoutineTypeIdConverter>();
        configurationBuilder.Properties<AlertId>().HaveConversion<AlertIdConverter>();
        configurationBuilder.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
        configurationBuilder.Properties<LicensePlate>().HaveConversion<LicensePlateConverter>();
        configurationBuilder.Properties<RoutineTypeCode>().HaveConversion<RoutineTypeCodeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(MaintenanceSchema.Name);
        modelBuilder.ApplyConfiguration(new RoutineTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenancePlanConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenanceRoutineConfiguration());
        modelBuilder.ApplyConfiguration(new AlertConfiguration());
        modelBuilder.ApplyConfiguration(new OdometerReadingRecordConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(MaintenanceSchema.Name));
        modelBuilder.ApplyConfiguration(new EventCheckpointConfiguration(MaintenanceSchema.Name));
        modelBuilder.ApplyRutaVivaConventions();
        modelBuilder.ApplyEnumCheckConstraints();
    }
}
