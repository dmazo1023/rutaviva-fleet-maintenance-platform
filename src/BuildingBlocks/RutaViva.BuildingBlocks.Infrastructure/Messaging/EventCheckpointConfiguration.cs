using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class EventCheckpointConfiguration(string schema) : IEntityTypeConfiguration<EventCheckpoint>
{
    private const string TableName = "EventCheckpoints";

    public void Configure(EntityTypeBuilder<EventCheckpoint> builder)
    {
        builder.ToTable(TableName, schema);
        builder.HasKey(checkpoint => checkpoint.SubscriberName);
        builder.Property(checkpoint => checkpoint.SubscriberName)
            .HasMaxLength(EventCheckpoint.SubscriberNameMaxLength)
            .IsUnicode(false)
            .ValueGeneratedNever();
    }
}
