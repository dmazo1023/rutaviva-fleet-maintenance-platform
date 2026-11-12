using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Configurations;

public sealed class ApproverConfiguration : IEntityTypeConfiguration<Approver>
{
    private const string TableName = "Approvers";

    public void Configure(EntityTypeBuilder<Approver> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureIndexes(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<Approver> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(approver => approver.Id);
        builder.Property(approver => approver.Id).ValueGeneratedNever();
    }

    private static void ConfigureColumns(EntityTypeBuilder<Approver> builder)
    {
        builder.Property(approver => approver.CustomerId).IsRequired();
        builder.Property(approver => approver.FullName).HasMaxLength(PersonName.MaxLength).IsRequired();
        builder.Property(approver => approver.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
        builder.Property(approver => approver.Status).IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Approver> builder)
    {
        builder.HasIndex(approver => new { approver.CustomerId, approver.Email })
            .IsUnique()
            .HasDatabaseName(CustomerIndexNames.ApproverEmail);
    }
}
