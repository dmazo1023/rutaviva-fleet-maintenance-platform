using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Infrastructure.Persistence;

public sealed class DeadLetterConfiguration : IEntityTypeConfiguration<DeadLetter>
{
    private const string TableName = "DeadLetters";

    public void Configure(EntityTypeBuilder<DeadLetter> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(deadLetter => deadLetter.Id);
        builder.Property(deadLetter => deadLetter.Id).UseIdentityColumn();
        builder.Property(deadLetter => deadLetter.SubscriberName).HasMaxLength(IntegrationEventLimits.SubscriberNameMaxLength).IsUnicode(false);
        builder.Property(deadLetter => deadLetter.Error).HasMaxLength(IntegrationEventLimits.FailureMaxLength);
    }
}
