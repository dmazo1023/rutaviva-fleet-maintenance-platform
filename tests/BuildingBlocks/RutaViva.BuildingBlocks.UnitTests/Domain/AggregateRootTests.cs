using System.Reflection;
using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.UnitTests.Domain;

public sealed class AggregateRootTests
{
    private const int EventCount = 2;

    [Fact]
    public void DomainEvents_Should_BeEmpty_When_NothingRaised()
    {
        TestAggregate aggregate = new();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raise_Should_AddEvent_When_EventIsValid()
    {
        TestAggregate aggregate = new();

        aggregate.Trigger(new TestEvent());
        aggregate.Trigger(new TestEvent());

        aggregate.DomainEvents.Should().HaveCount(EventCount);
    }

    [Fact]
    public void Raise_Should_Throw_When_EventIsNull()
    {
        TestAggregate aggregate = new();

        Action act = () => aggregate.Trigger(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearDomainEvents_Should_RemoveEvents_When_EventsWereRaised()
    {
        TestAggregate aggregate = new();
        aggregate.Trigger(new TestEvent());

        ((IAggregateRoot)aggregate).ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_Should_NotBePublic_When_ExposedByAggregateRoot()
    {
        MethodInfo? publicMethod = typeof(AggregateRoot<Guid>).GetMethod(
            nameof(IAggregateRoot.ClearDomainEvents),
            BindingFlags.Public | BindingFlags.Instance);

        publicMethod.Should().BeNull();
    }

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public void Trigger(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    private sealed record TestEvent : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }
}
