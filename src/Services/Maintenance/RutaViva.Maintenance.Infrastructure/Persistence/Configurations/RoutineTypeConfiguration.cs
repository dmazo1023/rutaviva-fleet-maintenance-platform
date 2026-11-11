using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Configurations;

public sealed class RoutineTypeConfiguration : IEntityTypeConfiguration<RoutineType>
{
    private const string TableName = "RoutineTypes";
    private const string CodeIndexName = "UX_RoutineTypes_Code";
    private const string OwnerKeyName = "RoutineTypeId";
    private const string ThresholdColumn = "DueSoonThresholdPercent";
    private const string IntervalKilometersColumn = "DefaultIntervalKm";
    private const string IntervalMonthsColumn = "DefaultIntervalMonths";
    private const string MonthsColumnType = "smallint";
    private const int CodeMaxLength = 30;
    private const int NameMaxLength = 100;
    private const int UpdatedByMaxLength = 256;
    private const int PercentPrecision = 5;
    private const int PercentScale = 2;

    public void Configure(EntityTypeBuilder<RoutineType> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureInterval(builder);
        builder.HasData(RoutineTypeSeed.TypeRows());
    }

    private static void ConfigureTable(EntityTypeBuilder<RoutineType> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(routineType => routineType.Id);
        builder.Property(routineType => routineType.Id).ValueGeneratedNever();
        builder.Ignore(routineType => routineType.DomainEvents);
    }

    private static void ConfigureColumns(EntityTypeBuilder<RoutineType> builder)
    {
        builder.Property(routineType => routineType.Code)
            .HasMaxLength(CodeMaxLength)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(routineType => routineType.Code).IsUnique().HasDatabaseName(CodeIndexName);
        builder.Property(routineType => routineType.Name).HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(routineType => routineType.DueSoonThreshold)
            .HasColumnName(ThresholdColumn)
            .HasConversion(threshold => threshold.Percent, percent => DueSoonThreshold.Create(percent).Value)
            .HasPrecision(PercentPrecision, PercentScale);
        builder.Property(routineType => routineType.UpdatedBy).HasMaxLength(UpdatedByMaxLength).IsRequired();
    }

    private static void ConfigureInterval(EntityTypeBuilder<RoutineType> builder)
    {
        builder.OwnsOne(routineType => routineType.DefaultInterval, ConfigureIntervalColumns);
        builder.Navigation(routineType => routineType.DefaultInterval).IsRequired();
    }

    private static void ConfigureIntervalColumns(OwnedNavigationBuilder<RoutineType, MaintenanceInterval> owned)
    {
        owned.WithOwner().HasForeignKey(OwnerKeyName);
        owned.Property(interval => interval.Kilometers).HasColumnName(IntervalKilometersColumn);
        owned.Property(interval => interval.Months).HasColumnName(IntervalMonthsColumn).HasColumnType(MonthsColumnType);
        owned.HasData(RoutineTypeSeed.IntervalRows());
    }
}
