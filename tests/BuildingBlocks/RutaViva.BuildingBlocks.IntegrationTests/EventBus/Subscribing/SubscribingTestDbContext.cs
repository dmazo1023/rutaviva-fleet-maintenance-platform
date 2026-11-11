using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class SubscribingTestDbContext(DbContextOptions<SubscribingTestDbContext> options) : DbContext(options)
{
    public const string Schema = "subscribing";

    private const string EffectsTable = "Effects";

    public DbSet<ProcessedEffect> Effects => Set<ProcessedEffect>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedEffect>(builder =>
        {
            builder.ToTable(EffectsTable, Schema);
            builder.HasKey(effect => effect.Id);
            builder.HasIndex(effect => effect.MessageId).IsUnique();
            builder.Property(effect => effect.Subject).HasMaxLength(ProcessedEffect.TextMaxLength);
            builder.Property(effect => effect.EventType).HasMaxLength(ProcessedEffect.TextMaxLength);
        });
        modelBuilder.ApplyConfiguration(new EventCheckpointConfiguration(Schema));
        modelBuilder.ApplyRutaVivaConventions();
    }
}
