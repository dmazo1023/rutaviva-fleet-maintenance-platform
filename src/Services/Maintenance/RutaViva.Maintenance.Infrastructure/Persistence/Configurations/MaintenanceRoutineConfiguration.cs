using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceRoutineConfiguration : IEntityTypeConfiguration<MaintenanceRoutine>
{
    private const string TableName = "MaintenanceRoutines";
    private const string VehicleIdColumn = "VehicleId";
    private const string UniqueIndexName = "UX_MaintenanceRoutines_VehicleId_RoutineTypeId";
    private const string OverrideIntervalKilometersColumn = "IntervalKm";
    private const string OverrideIntervalMonthsColumn = "IntervalMonths";
    private const string OverridePercentColumn = "DueSoonThresholdPercentOverride";
    private const string CycleNumberColumn = "CycleNumber";
    private const string CycleStartKilometersColumn = "CycleStartKm";
    private const string CycleStartDateColumn = "CycleStartDate";
    private const string MonthsColumnType = "smallint";
    private const int CodeMaxLength = 30;
    private const int NameMaxLength = 100;
    private const int PercentPrecision = 5;
    private const int PercentScale = 2;

    public void Configure(EntityTypeBuilder<MaintenanceRoutine> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureOverrides(builder);
        ConfigureCycle(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<MaintenanceRoutine> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(routine => routine.Id);
        builder.Property(routine => routine.Id).ValueGeneratedNever();
        builder.Ignore(routine => routine.IsActive);
    }

    private static void ConfigureColumns(EntityTypeBuilder<MaintenanceRoutine> builder)
    {
        builder.Property<VehicleId>(VehicleIdColumn).IsRequired();
        builder.Property(routine => routine.RoutineTypeId).IsRequired();
        builder.Property(routine => routine.RoutineTypeCode).HasMaxLength(CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(routine => routine.Name).HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(routine => routine.State).IsRequired();
        builder.Property(routine => routine.DueStatus).IsRequired();
        builder.Property(routine => routine.DueSoonNotified).IsRequired();
    }

    private static void ConfigureOverrides(EntityTypeBuilder<MaintenanceRoutine> builder)
    {
        builder.ComplexProperty(routine => routine.Overrides, ConfigureOverrideColumns);
    }

    private static void ConfigureOverrideColumns(ComplexPropertyBuilder<RoutineOverrides> complex)
    {
        complex.Property(overrides => overrides.IntervalKm).HasColumnName(OverrideIntervalKilometersColumn);
        complex.Property(overrides => overrides.IntervalMonths)
            .HasColumnName(OverrideIntervalMonthsColumn)
            .HasColumnType(MonthsColumnType);
        complex.Property(overrides => overrides.DueSoonPercent)
            .HasColumnName(OverridePercentColumn)
            .HasPrecision(PercentPrecision, PercentScale);
    }

    private static void ConfigureCycle(EntityTypeBuilder<MaintenanceRoutine> builder)
    {
        builder.ComplexProperty(routine => routine.Cycle, ConfigureCycleColumns);
    }

    private static void ConfigureCycleColumns(ComplexPropertyBuilder<MaintenanceCycle> complex)
    {
        complex.Property(cycle => cycle.Number).HasColumnName(CycleNumberColumn);
        complex.Property(cycle => cycle.StartKm).HasColumnName(CycleStartKilometersColumn);
        complex.Property(cycle => cycle.StartDate).HasColumnName(CycleStartDateColumn);
    }

    private static void ConfigureRelationships(EntityTypeBuilder<MaintenanceRoutine> builder)
    {
        builder.HasOne<RoutineType>()
            .WithMany()
            .HasForeignKey(routine => routine.RoutineTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(VehicleIdColumn, nameof(MaintenanceRoutine.RoutineTypeId))
            .IsUnique()
            .HasDatabaseName(UniqueIndexName);
    }
}
