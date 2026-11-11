namespace RutaViva.BuildingBlocks.Infrastructure.Persistence;

public interface IHasRowVersion
{
    byte[] RowVersion { get; }
}
