using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Configurations;

public sealed class VehicleReferenceConfiguration : IEntityTypeConfiguration<VehicleReference>
{
    private const string TableName = "VehicleReferences";
    private const string KeyColumn = "VehicleId";

    public void Configure(EntityTypeBuilder<VehicleReference> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Property(reference => reference.Plate).HasMaxLength(ServiceOrder.MaxPlateLength).IsRequired();
        builder.Property(reference => reference.CustomerId).IsRequired();
        builder.Property(reference => reference.WorkshopId).IsRequired();
        builder.Property(reference => reference.IsActive).IsRequired();
        builder.Property(reference => reference.UpdatedAt).IsRequired();
    }
}

public sealed class WorkshopReferenceConfiguration : IEntityTypeConfiguration<WorkshopReference>
{
    private const string TableName = "WorkshopReferences";
    private const string KeyColumn = "WorkshopId";

    public void Configure(EntityTypeBuilder<WorkshopReference> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Property(reference => reference.Name).HasMaxLength(ServiceOrder.MaxTextLength).IsRequired();
        builder.Property(reference => reference.UpdatedAt).IsRequired();
    }
}

public sealed class RoutineReferenceConfiguration : IEntityTypeConfiguration<RoutineReference>
{
    private const string TableName = "RoutineReferences";
    private const string KeyColumn = "RoutineId";

    public void Configure(EntityTypeBuilder<RoutineReference> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Property(reference => reference.VehicleId).IsRequired();
        builder.Property(reference => reference.RoutineName).HasMaxLength(ServiceOrder.MaxTextLength).IsRequired();
        builder.Property(reference => reference.RoutineTypeCode).HasMaxLength(ServiceOrder.MaxRoutineTypeCodeLength).IsRequired();
        builder.Property(reference => reference.UpdatedAt).IsRequired();
    }
}

public sealed class ApproverReferenceConfiguration : IEntityTypeConfiguration<ApproverReference>
{
    private const string TableName = "ApproverReferences";
    private const string KeyColumn = "ApproverId";

    public void Configure(EntityTypeBuilder<ApproverReference> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Property(reference => reference.CustomerId).IsRequired();
        builder.Property(reference => reference.FullName).HasMaxLength(ServiceOrder.MaxTextLength).IsRequired();
        builder.Property(reference => reference.IsActive).IsRequired();
        builder.Property(reference => reference.UpdatedAt).IsRequired();
    }
}
