using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxMessageConfiguration(string schema) : IEntityTypeConfiguration<OutboxMessage>
{
    private const string TableName = "OutboxMessages";
    private const string PendingIndexName = "IX_OutboxMessages_Pending";
    private const string PendingFilter = "[ProcessedAt] IS NULL";

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ConfigureTable(builder);
        ConfigureColumns(builder);
        ConfigurePendingIndex(builder);
    }

    private void ConfigureTable(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(TableName, schema);
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
    }

    private static void ConfigureColumns(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.Property(message => message.Stream).HasMaxLength(OutboxMessage.StreamMaxLength).IsUnicode(false);
        builder.Property(message => message.EventType).HasMaxLength(OutboxMessage.EventTypeMaxLength).IsUnicode(false);
        builder.Property(message => message.Subject).HasMaxLength(OutboxMessage.SubjectMaxLength).IsUnicode(false);
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(OutboxMessage.LastErrorMaxLength);
    }

    private static void ConfigurePendingIndex(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasIndex(message => message.OccurredAt)
            .HasDatabaseName(PendingIndexName)
            .HasFilter(PendingFilter);
    }
}
