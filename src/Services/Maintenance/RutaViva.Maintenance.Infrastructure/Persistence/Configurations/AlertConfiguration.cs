using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    private const string TableName = "Alerts";
    private const string TriggerColumn = "TriggeredBy";
    private const string OnePerCycleIndexName = "UX_Alerts_OnePerCycleAndType";
    private const string StatusRaisedAtIndexName = "IX_Alerts_Status_RaisedAt";
    private const string CustomerStatusIndexName = "IX_Alerts_CustomerId_Status";
    private const string VehicleIndexName = "IX_Alerts_VehicleId";
    private const int PlateMaxLength = 10;
    private const int RoutineNameMaxLength = 100;
    private const int AcknowledgedByMaxLength = 256;

    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(alert => alert.Id);
        builder.Property(alert => alert.Id).ValueGeneratedNever();
        builder.Ignore(alert => alert.DomainEvents);
    }

    private static void ConfigureColumns(EntityTypeBuilder<Alert> builder)
    {
        builder.Property(alert => alert.VehicleId).IsRequired();
        builder.Property(alert => alert.Plate).HasMaxLength(PlateMaxLength).IsUnicode(false).IsRequired();
        builder.Property(alert => alert.CustomerId).IsRequired();
        builder.Property(alert => alert.RoutineId).IsRequired();
        builder.Property(alert => alert.RoutineName).HasMaxLength(RoutineNameMaxLength).IsRequired();
        builder.Property(alert => alert.CycleNumber).IsRequired();
        builder.Property(alert => alert.Type).IsRequired();
        builder.Property(alert => alert.Trigger).HasColumnName(TriggerColumn).IsRequired();
        builder.Property(alert => alert.Status).IsRequired();
        builder.Property(alert => alert.RaisedAt).IsRequired();
        builder.Property(alert => alert.AcknowledgedBy).HasMaxLength(AcknowledgedByMaxLength);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Alert> builder)
    {
        builder.HasIndex(alert => new { alert.RoutineId, alert.CycleNumber, alert.Type })
            .IsUnique()
            .HasDatabaseName(OnePerCycleIndexName);
        builder.HasIndex(alert => new { alert.Status, alert.RaisedAt })
            .IsDescending(false, true)
            .HasDatabaseName(StatusRaisedAtIndexName);
        builder.HasIndex(alert => new { alert.CustomerId, alert.Status }).HasDatabaseName(CustomerStatusIndexName);
        builder.HasIndex(alert => alert.VehicleId).HasDatabaseName(VehicleIndexName);
    }

    private static void ConfigureRelationships(EntityTypeBuilder<Alert> builder)
    {
        builder.HasOne<MaintenanceRoutine>()
            .WithMany()
            .HasForeignKey(alert => alert.RoutineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
