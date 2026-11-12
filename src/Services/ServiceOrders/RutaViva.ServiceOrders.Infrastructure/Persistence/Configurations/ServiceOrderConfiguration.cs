using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.ServiceOrders.Domain;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    private const string TableName = "ServiceOrders";
    private const string HistoryTableName = "ServiceOrderHistory";
    private const string HistoryForeignKey = "ServiceOrderId";
    private const string NumberIndexName = "UX_ServiceOrders_Number";
    private const string VehicleIndexName = "IX_ServiceOrders_VehicleId";
    private const string VehicleIdColumn = "VehicleId";
    private const string PlateColumn = "Plate";
    private const string CustomerIdColumn = "CustomerId";
    private const string WorkshopIdColumn = "WorkshopId";
    private const string WorkshopNameColumn = "WorkshopName";
    private const string RoutineIdColumn = "RoutineId";
    private const string RoutineNameColumn = "RoutineName";
    private const string RoutineTypeCodeColumn = "RoutineTypeCode";
    private const string StatusColumn = "Status";
    private const int StatusMaxLength = 30;
    private const int AmountPrecision = 18;
    private const int AmountScale = 2;

    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureReference(builder);
        ConfigureHistory(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Ignore(order => order.DomainEvents);
        builder.HasIndex(order => order.Number).IsUnique().HasDatabaseName(NumberIndexName);
    }

    private static void ConfigureColumns(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.Property(order => order.Number).HasMaxLength(ServiceOrder.MaxNumberLength).IsUnicode(false).IsRequired();
        builder.Property(order => order.Status).IsRequired();
        builder.Property(order => order.QuotedAmount).HasPrecision(AmountPrecision, AmountScale);
        builder.Property(order => order.AutoApprovalMaxAmount).HasPrecision(AmountPrecision, AmountScale);
        builder.Property(order => order.AutoApproved).IsRequired();
        builder.Property(order => order.CreatedAt).IsRequired();
        builder.Property(order => order.UpdatedAt).IsRequired();
    }

    private static void ConfigureReference(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.OwnsOne(order => order.Reference, ConfigureReferenceColumns);
        builder.Navigation(order => order.Reference).IsRequired();
    }

    private static void ConfigureReferenceColumns(OwnedNavigationBuilder<ServiceOrder, ServiceOrderReference> reference)
    {
        reference.Property(value => value.VehicleId).HasColumnName(VehicleIdColumn).IsRequired();
        reference.Property(value => value.Plate).HasColumnName(PlateColumn).HasMaxLength(ServiceOrder.MaxPlateLength).IsRequired();
        reference.Property(value => value.CustomerId).HasColumnName(CustomerIdColumn).IsRequired();
        reference.Property(value => value.WorkshopId).HasColumnName(WorkshopIdColumn).IsRequired();
        reference.Property(value => value.WorkshopName).HasColumnName(WorkshopNameColumn).HasMaxLength(ServiceOrder.MaxTextLength).IsRequired();
        reference.Property(value => value.RoutineId).HasColumnName(RoutineIdColumn).IsRequired();
        reference.Property(value => value.RoutineName).HasColumnName(RoutineNameColumn).HasMaxLength(ServiceOrder.MaxTextLength).IsRequired();
        reference.Property(value => value.RoutineTypeCode).HasColumnName(RoutineTypeCodeColumn).HasMaxLength(ServiceOrder.MaxRoutineTypeCodeLength).IsRequired();
        reference.HasIndex(value => value.VehicleId).HasDatabaseName(VehicleIndexName);
    }

    private static void ConfigureHistory(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.OwnsMany(order => order.History, ConfigureHistoryColumns);
        builder.Navigation(order => order.History).HasField("_history").UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureHistoryColumns(OwnedNavigationBuilder<ServiceOrder, ServiceOrderHistoryEntry> history)
    {
        history.ToTable(HistoryTableName);
        history.WithOwner().HasForeignKey(HistoryForeignKey);
        history.HasKey(entry => entry.Id);
        history.Property(entry => entry.Id).ValueGeneratedNever();
        history.Property(entry => entry.Status).HasColumnName(StatusColumn).HasConversion<string>().HasMaxLength(StatusMaxLength).IsRequired();
        history.Property(entry => entry.ChangedBy).HasMaxLength(ServiceOrder.MaxTextLength).IsRequired();
        history.Property(entry => entry.ChangedAt).IsRequired();
    }
}
