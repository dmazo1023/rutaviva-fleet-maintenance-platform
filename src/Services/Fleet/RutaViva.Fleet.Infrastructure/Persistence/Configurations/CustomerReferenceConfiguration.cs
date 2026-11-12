using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Infrastructure.Persistence.Configurations;

public sealed class CustomerReferenceConfiguration : IEntityTypeConfiguration<CustomerReference>
{
    private const string TableName = "CustomerReferences";
    private const string KeyColumn = "CustomerId";
    private const string TaxIdIndexName = "UX_CustomerReferences_TaxId";
    private const int TaxIdMaxLength = 30;
    private const int NameMaxLength = 200;

    public void Configure(EntityTypeBuilder<CustomerReference> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Property(reference => reference.TaxId).HasMaxLength(TaxIdMaxLength).IsUnicode(false).IsRequired();
        builder.Property(reference => reference.Name).HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(reference => reference.Status).IsRequired();
        builder.Property(reference => reference.UpdatedAt).IsRequired();
        builder.HasIndex(reference => reference.TaxId).IsUnique().HasDatabaseName(TaxIdIndexName);
    }
}
