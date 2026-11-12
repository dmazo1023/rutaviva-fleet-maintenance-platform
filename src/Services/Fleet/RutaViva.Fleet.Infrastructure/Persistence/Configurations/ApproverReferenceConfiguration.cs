using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Infrastructure.Persistence.Configurations;

public sealed class ApproverReferenceConfiguration : IEntityTypeConfiguration<ApproverReference>
{
    private const string TableName = "ApproverReferences";
    private const string KeyColumn = "ApproverId";
    private const string EmailIndexName = "IX_ApproverReferences_Email";
    private const int FullNameMaxLength = 200;
    private const int EmailMaxLength = 256;

    public void Configure(EntityTypeBuilder<ApproverReference> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Id).HasColumnName(KeyColumn).ValueGeneratedNever();
        builder.Property(reference => reference.CustomerId).IsRequired();
        builder.Property(reference => reference.FullName).HasMaxLength(FullNameMaxLength).IsRequired();
        builder.Property(reference => reference.Email).HasMaxLength(EmailMaxLength).IsRequired();
        builder.Property(reference => reference.Status).IsRequired();
        builder.Property(reference => reference.UpdatedAt).IsRequired();
        builder.HasIndex(reference => reference.Email).HasDatabaseName(EmailIndexName);
    }
}
