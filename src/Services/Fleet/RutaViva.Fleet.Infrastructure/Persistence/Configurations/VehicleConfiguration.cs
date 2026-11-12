using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    internal const string PlateIndexName = "UX_Vehicles_Plate";
    internal const string VinIndexName = "UX_Vehicles_Vin";

    private const string TableName = "Vehicles";
    private const string KeyColumn = "VehicleId";
    private const string BrandColumn = "Brand";
    private const string ModelColumn = "Model";
    private const string ModelYearColumn = "ModelYear";
    private const string InitialOdometerColumn = "InitialOdometerKm";
    private const string VinFilter = "[Vin] IS NOT NULL";
    private const string CustomerStatusIndexName = "IX_Vehicles_CustomerId_Status";
    private const int PlateMaxLength = 6;
    private const int VinMaxLength = 17;
    private const int DescriptionMaxLength = 100;

    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureDescription(builder);
        ConfigureIndexes(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Ignore(vehicle => vehicle.DomainEvents);
    }

    private static void ConfigureColumns(EntityTypeBuilder<Vehicle> builder)
    {
        builder.Property(vehicle => vehicle.Plate).HasMaxLength(PlateMaxLength).IsUnicode(false).IsRequired();
        builder.Property(vehicle => vehicle.Vin).HasMaxLength(VinMaxLength).IsUnicode(false);
        builder.Property(vehicle => vehicle.InitialOdometer).HasColumnName(InitialOdometerColumn).IsRequired();
        builder.Property(vehicle => vehicle.CustomerId).IsRequired();
        builder.Property(vehicle => vehicle.WorkshopId).IsRequired();
        builder.Property(vehicle => vehicle.Status).IsRequired();
        builder.Property(vehicle => vehicle.RegisteredAt).IsRequired();
        builder.Property(vehicle => vehicle.StatusChangedAt).IsRequired();
    }

    private static void ConfigureDescription(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ComplexProperty(vehicle => vehicle.Description, ConfigureDescriptionColumns);
    }

    private static void ConfigureDescriptionColumns(ComplexPropertyBuilder<VehicleDescription> complex)
    {
        complex.Property(description => description.Brand).HasColumnName(BrandColumn).HasMaxLength(DescriptionMaxLength);
        complex.Property(description => description.Model).HasColumnName(ModelColumn).HasMaxLength(DescriptionMaxLength);
        complex.Property(description => description.ModelYear).HasColumnName(ModelYearColumn);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasIndex(vehicle => vehicle.Plate).IsUnique().HasDatabaseName(PlateIndexName);
        builder.HasIndex(vehicle => vehicle.Vin).IsUnique().HasFilter(VinFilter).HasDatabaseName(VinIndexName);
        builder.HasIndex(vehicle => new { vehicle.CustomerId, vehicle.Status }).HasDatabaseName(CustomerStatusIndexName);
    }
}
