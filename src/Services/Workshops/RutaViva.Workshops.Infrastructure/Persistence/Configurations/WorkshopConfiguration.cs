using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Infrastructure.Persistence.Configurations;

public sealed class WorkshopConfiguration : IEntityTypeConfiguration<Workshop>
{
    private const string TableName = "Workshops";
    private const string TaxIdIndexName = "UX_Workshops_TaxId";
    private const string AddressColumn = "Address";
    private const string CityColumn = "City";
    private const int PhoneMaxLength = PhoneNumber.MaxDigits + 1;

    public void Configure(EntityTypeBuilder<Workshop> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureIndexes(builder);
        ConfigureAddress(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<Workshop> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(workshop => workshop.Id);
        builder.Property(workshop => workshop.Id).ValueGeneratedNever();
        builder.Ignore(workshop => workshop.DomainEvents);
    }

    private static void ConfigureColumns(EntityTypeBuilder<Workshop> builder)
    {
        builder.Property(workshop => workshop.Name).HasMaxLength(Workshop.MaxNameLength).IsRequired();
        builder.Property(workshop => workshop.TaxId).HasMaxLength(TaxId.MaxLength).IsUnicode(false).IsRequired();
        builder.Property(workshop => workshop.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
        builder.Property(workshop => workshop.Phone).HasMaxLength(PhoneMaxLength).IsUnicode(false);
        builder.Property(workshop => workshop.Status).IsRequired();
        builder.Property(workshop => workshop.CreatedAt).IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Workshop> builder)
    {
        builder.HasIndex(workshop => workshop.TaxId).IsUnique().HasDatabaseName(TaxIdIndexName);
    }

    private static void ConfigureAddress(EntityTypeBuilder<Workshop> builder)
    {
        builder.OwnsOne(workshop => workshop.Address, ConfigureAddressColumns);
        builder.Navigation(workshop => workshop.Address).IsRequired();
    }

    private static void ConfigureAddressColumns(OwnedNavigationBuilder<Workshop, PostalAddress> address)
    {
        address.Property(postal => postal.Address)
            .HasColumnName(AddressColumn)
            .HasMaxLength(PostalAddress.MaxAddressLength)
            .IsRequired();
        address.Property(postal => postal.City)
            .HasColumnName(CityColumn)
            .HasMaxLength(PostalAddress.MaxCityLength)
            .IsRequired();
    }
}
