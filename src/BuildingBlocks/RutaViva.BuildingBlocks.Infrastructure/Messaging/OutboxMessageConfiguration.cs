using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RutaViva.IntegrationContracts;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxMessageConfiguration(string schema) : IEntityTypeConfiguration<OutboxMessage>
{
    private const string TableName = "OutboxMessages";
    private const string PendingIndexName = "IX_OutboxMessages_Pending";
    private const string PendingFilter = "[ProcessedAt] IS NULL AND [FailedAt] IS NULL";

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
        builder.Property(message => message.Sequence).ValueGeneratedOnAdd();
    }

    private static void ConfigureColumns(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.Property(message => message.Stream).HasMaxLength(IntegrationEventLimits.StreamMaxLength).IsUnicode(false);
        builder.Property(message => message.EventType).HasMaxLength(IntegrationEventLimits.TypeMaxLength).IsUnicode(false);
        builder.Property(message => message.Subject).HasMaxLength(IntegrationEventLimits.SubjectMaxLength).IsUnicode(false);
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(OutboxMessage.LastErrorMaxLength);
    }

    private static void ConfigurePendingIndex(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasIndex(message => new { message.ProcessedAt, message.FailedAt, message.Sequence })
            .HasDatabaseName(PendingIndexName)
            .HasFilter(PendingFilter);
    }
}
