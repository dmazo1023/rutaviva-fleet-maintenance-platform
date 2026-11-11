using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Publishing;

public sealed class OutboxRelayTestDbContext(DbContextOptions<OutboxRelayTestDbContext> options) : DbContext(options)
{
    public const string Schema = "relay";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
        modelBuilder.ApplyRutaVivaConventions();
    }
}
