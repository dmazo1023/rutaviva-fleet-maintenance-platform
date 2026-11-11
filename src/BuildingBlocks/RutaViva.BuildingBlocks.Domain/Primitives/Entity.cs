namespace RutaViva.BuildingBlocks.Domain.Primitives;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return HasSameIdentity(other);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    private bool HasSameIdentity(Entity<TId> other)
    {
        if (GetType() != other.GetType())
        {
            return false;
        }

        if (HasDefaultId())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    private bool HasDefaultId() => EqualityComparer<TId>.Default.Equals(Id, default!);
}
