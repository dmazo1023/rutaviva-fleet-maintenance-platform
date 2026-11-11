using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RutaViva.EventBus.Infrastructure.Persistence;

public sealed class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    private const string TableName = "Events";
    private const string MessageIdIndexName = "UX_Events_MessageId";
    private const string StreamOffsetIndexName = "IX_Events_Stream_Offset";

    public void Configure(EntityTypeBuilder<StoredEvent> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigureIndexes(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(stored => stored.Offset);
        builder.Property(stored => stored.Offset).UseIdentityColumn();
    }

    private static void ConfigureColumns(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.Property(stored => stored.Stream).HasMaxLength(StoredEvent.StreamMaxLength).IsUnicode(false);
        builder.Property(stored => stored.Type).HasMaxLength(StoredEvent.TypeMaxLength).IsUnicode(false);
        builder.Property(stored => stored.Subject).HasMaxLength(StoredEvent.SubjectMaxLength).IsUnicode(false);
        builder.Property(stored => stored.Payload).IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.HasIndex(stored => stored.MessageId).IsUnique().HasDatabaseName(MessageIdIndexName);
        builder.HasIndex(stored => new { stored.Stream, stored.Offset }).HasDatabaseName(StreamOffsetIndexName);
    }
}
