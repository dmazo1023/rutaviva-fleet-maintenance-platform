using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    private const string TableName = "Customers";
    private const string StatusIndexName = "IX_Customers_Status";
    private const string ApproversField = "_approvers";
    private const int TaxIdMaxLength = 20;
    private const int PhoneMaxLength = 16;

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureIndexes(builder);
        ConfigureApprovers(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();
        builder.Ignore(customer => customer.DomainEvents);
    }

    private static void ConfigureColumns(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(customer => customer.Name).HasMaxLength(PersonName.MaxLength).IsRequired();
        builder.Property(customer => customer.TaxId).HasMaxLength(TaxIdMaxLength).IsUnicode(false).IsRequired();
        builder.Property(customer => customer.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
        builder.Property(customer => customer.Phone).HasMaxLength(PhoneMaxLength).IsUnicode(false);
        builder.Property(customer => customer.Status).IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(customer => customer.TaxId).IsUnique().HasDatabaseName(CustomerIndexNames.TaxId);
        builder.HasIndex(customer => customer.Status).HasDatabaseName(StatusIndexName);
    }

    private static void ConfigureApprovers(EntityTypeBuilder<Customer> builder)
    {
        builder.HasMany(customer => customer.Approvers)
            .WithOne()
            .HasForeignKey(approver => approver.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(customer => customer.Approvers)
            .HasField(ApproversField)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
