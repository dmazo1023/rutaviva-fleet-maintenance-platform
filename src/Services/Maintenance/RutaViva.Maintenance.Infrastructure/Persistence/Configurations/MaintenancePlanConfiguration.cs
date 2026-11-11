using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Configurations;

public sealed class MaintenancePlanConfiguration : IEntityTypeConfiguration<MaintenancePlan>
{
    private const string TableName = "MaintenancePlans";
    private const string KeyColumn = "VehicleId";
    private const string PlateIndexName = "UX_MaintenancePlans_Plate";
    private const string RoutinesField = "_routines";
    private const string OdometerKilometersColumn = "OdometerKm";
    private const string OdometerReadAtColumn = "OdometerReadAt";
    private const int PlateMaxLength = 10;
    private const int TimestampPrecision = 0;

    public void Configure(EntityTypeBuilder<MaintenancePlan> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureOdometer(builder);
        ConfigureRoutines(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<MaintenancePlan> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Ignore(plan => plan.DomainEvents);
    }

    private static void ConfigureColumns(EntityTypeBuilder<MaintenancePlan> builder)
    {
        builder.Property(plan => plan.Plate).HasMaxLength(PlateMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(plan => plan.Plate).IsUnique().HasDatabaseName(PlateIndexName);
        builder.Property(plan => plan.CustomerId).IsRequired();
        builder.Property(plan => plan.Status).IsRequired();
        builder.Property(plan => plan.LastTimeEvaluationDate).IsRequired();
    }

    private static void ConfigureOdometer(EntityTypeBuilder<MaintenancePlan> builder)
    {
        builder.ComplexProperty(plan => plan.Odometer, ConfigureOdometerColumns);
    }

    private static void ConfigureOdometerColumns(ComplexPropertyBuilder<Odometer> complex)
    {
        complex.Property(odometer => odometer.Kilometers).HasColumnName(OdometerKilometersColumn);
        complex.Property(odometer => odometer.ReadAt).HasColumnName(OdometerReadAtColumn).HasPrecision(TimestampPrecision);
    }

    private static void ConfigureRoutines(EntityTypeBuilder<MaintenancePlan> builder)
    {
        builder.HasMany(plan => plan.Routines)
            .WithOne()
            .HasForeignKey(KeyColumn)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(plan => plan.Routines)
            .HasField(RoutinesField)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
