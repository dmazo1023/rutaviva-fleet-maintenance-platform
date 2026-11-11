using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Configurations;

public sealed class OdometerReadingRecordConfiguration : IEntityTypeConfiguration<OdometerReadingRecord>
{
    private const string TableName = "OdometerReadings";
    private const string KilometersCheckName = "CK_OdometerReadings_Kilometers";
    private const string KilometersCheckSql = "[Kilometers] >= 0";
    private const string VehicleReceivedAtIndexName = "IX_OdometerReadings_VehicleId_ReceivedAt";
    private const int IgnoredReasonMaxLength = 30;

    public void Configure(EntityTypeBuilder<OdometerReadingRecord> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureIndex(builder);
        ConfigureRelationship(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<OdometerReadingRecord> builder)
    {
        builder.ToTable(TableName, table => table.HasCheckConstraint(KilometersCheckName, KilometersCheckSql));
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedOnAdd();
    }

    private static void ConfigureColumns(EntityTypeBuilder<OdometerReadingRecord> builder)
    {
        builder.Property(record => record.VehicleId).IsRequired();
        builder.Property(record => record.Kilometers).IsRequired();
        builder.Property(record => record.ReadAt).IsRequired();
        builder.Property(record => record.ReceivedAt).IsRequired();
        builder.Property(record => record.Outcome).IsRequired();
        builder.Property(record => record.IgnoredReason)
            .HasConversion<string>()
            .HasMaxLength(IgnoredReasonMaxLength)
            .IsUnicode(false);
    }

    private static void ConfigureIndex(EntityTypeBuilder<OdometerReadingRecord> builder)
    {
        builder.HasIndex(record => new { record.VehicleId, record.ReceivedAt })
            .IsDescending(false, true)
            .HasDatabaseName(VehicleReceivedAtIndexName);
    }

    private static void ConfigureRelationship(EntityTypeBuilder<OdometerReadingRecord> builder)
    {
        builder.HasOne<MaintenancePlan>()
            .WithMany()
            .HasForeignKey(record => record.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
