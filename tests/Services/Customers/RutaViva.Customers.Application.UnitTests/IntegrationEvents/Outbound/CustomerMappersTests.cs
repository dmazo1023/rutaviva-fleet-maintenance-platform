using RutaViva.Customers.Application.IntegrationEvents.Outbound;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.Events;
using RutaViva.Customers.Domain.ValueObjects;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Customers.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class CustomerMappersTests
{
    [Fact]
    public void Map_Should_CopyAllFields_When_CustomerRegistered()
    {
        CustomerRegistered domainEvent = new(
            CustomerId.New(),
            TaxId.Create(CustomerTestFactory.TaxIdText).Value,
            PersonName.Create(CustomerTestFactory.CustomerName).Value,
            EmailAddress.Create(CustomerTestFactory.CustomerEmail).Value,
            CustomerStatus.Active,
            CustomerTestFactory.OccurredAt());

        CustomerRegisteredV1 result = (CustomerRegisteredV1)new CustomerRegisteredMapper().Map(domainEvent).Single();

        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.TaxId.Should().Be(CustomerTestFactory.TaxIdText);
        result.Name.Should().Be(CustomerTestFactory.CustomerName);
        result.Email.Should().Be(CustomerTestFactory.CustomerEmail);
        result.Status.Should().Be(nameof(CustomerStatus.Active));
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyAllFields_When_CustomerStatusChanged()
    {
        CustomerStatusChanged domainEvent = new(CustomerId.New(), CustomerStatus.Inactive, CustomerTestFactory.OccurredAt());

        CustomerStatusChangedV1 result = (CustomerStatusChangedV1)new CustomerStatusChangedMapper().Map(domainEvent).Single();

        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.Status.Should().Be(nameof(CustomerStatus.Inactive));
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyAllFields_When_ApproverRegistered()
    {
        ApproverRegistered domainEvent = new(
            ApproverId.New(),
            CustomerId.New(),
            PersonName.Create(CustomerTestFactory.ApproverName).Value,
            EmailAddress.Create(CustomerTestFactory.ApproverEmail).Value,
            ApproverStatus.Active,
            CustomerTestFactory.OccurredAt());

        ApproverRegisteredV1 result = (ApproverRegisteredV1)new ApproverRegisteredMapper().Map(domainEvent).Single();

        result.ApproverId.Should().Be(domainEvent.ApproverId.Value);
        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.FullName.Should().Be(CustomerTestFactory.ApproverName);
        result.Email.Should().Be(CustomerTestFactory.ApproverEmail);
        result.Status.Should().Be(nameof(ApproverStatus.Active));
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyAllFields_When_ApproverStatusChanged()
    {
        ApproverStatusChanged domainEvent = new(ApproverId.New(), CustomerId.New(), ApproverStatus.Inactive, CustomerTestFactory.OccurredAt());

        ApproverStatusChangedV1 result = (ApproverStatusChangedV1)new ApproverStatusChangedMapper().Map(domainEvent).Single();

        result.ApproverId.Should().Be(domainEvent.ApproverId.Value);
        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.Status.Should().Be(nameof(ApproverStatus.Inactive));
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_UseCustomerStream_When_CustomerRegistered()
    {
        CustomerRegistered domainEvent = new(
            CustomerId.New(),
            TaxId.Create(CustomerTestFactory.TaxIdText).Value,
            PersonName.Create(CustomerTestFactory.CustomerName).Value,
            EmailAddress.Create(CustomerTestFactory.CustomerEmail).Value,
            CustomerStatus.Active,
            CustomerTestFactory.OccurredAt());

        Type eventType = new CustomerRegisteredMapper().Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.CustomersCustomer);
    }
}
