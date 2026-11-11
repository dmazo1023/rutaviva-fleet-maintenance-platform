using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Domain.UnitTests.References;

public sealed class ReferenceTests
{
    private const string TaxId = "900000001";
    private const string NewTaxId = "900000002";
    private const string Name = "Transportes Andinos S.A.";
    private const string NewName = "RutaViva S.A.S.";
    private const string Email = "approver@rutaviva.test";
    private const string NewEmail = "other@rutaviva.test";
    private const int Year = 2026;
    private const int Month = 11;
    private const int Day = 12;

    private static readonly DateTimeOffset CreatedAt = new(Year, Month, Day, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = CreatedAt.AddHours(1);

    [Fact]
    public void Create_Should_ExposeEventData_When_CustomerReferenceIsBuilt()
    {
        CustomerId id = CustomerId.New();

        CustomerReference reference = CustomerReference.Create(id, TaxId, Name, ReferenceStatus.Active, CreatedAt);

        reference.Id.Should().Be(id);
        reference.TaxId.Should().Be(TaxId);
        reference.Name.Should().Be(Name);
        reference.Status.Should().Be(ReferenceStatus.Active);
        reference.UpdatedAt.Should().Be(CreatedAt);
        reference.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Apply_Should_UpdateOnlyStatusAndTimestamp_When_CustomerStatusChanges()
    {
        CustomerReference reference = CustomerReference.Create(CustomerId.New(), TaxId, Name, ReferenceStatus.Active, CreatedAt);

        reference.Apply(ReferenceStatus.Inactive, UpdatedAt);

        reference.IsActive.Should().BeFalse();
        reference.Name.Should().Be(Name);
        reference.TaxId.Should().Be(TaxId);
        reference.UpdatedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void Apply_Should_BeIdempotent_When_CustomerRegistrationIsAppliedTwice()
    {
        CustomerReference reference = CustomerReference.Create(CustomerId.New(), TaxId, Name, ReferenceStatus.Inactive, CreatedAt);

        reference.Apply(NewTaxId, NewName, ReferenceStatus.Active, UpdatedAt);
        reference.Apply(NewTaxId, NewName, ReferenceStatus.Active, UpdatedAt);

        reference.TaxId.Should().Be(NewTaxId);
        reference.Name.Should().Be(NewName);
        reference.IsActive.Should().BeTrue();
        reference.UpdatedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void Apply_Should_UpdateAllColumns_When_WorkshopRegistrationIsApplied()
    {
        WorkshopReference reference = WorkshopReference.Create(WorkshopId.New(), TaxId, Name, ReferenceStatus.Active, CreatedAt);

        reference.Apply(NewTaxId, NewName, ReferenceStatus.Inactive, UpdatedAt);

        reference.TaxId.Should().Be(NewTaxId);
        reference.Name.Should().Be(NewName);
        reference.IsActive.Should().BeFalse();
        reference.UpdatedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void Apply_Should_UpdateOnlyStatusAndTimestamp_When_WorkshopStatusChanges()
    {
        WorkshopReference reference = WorkshopReference.Create(WorkshopId.New(), TaxId, Name, ReferenceStatus.Inactive, CreatedAt);

        reference.Apply(ReferenceStatus.Active, UpdatedAt);

        reference.IsActive.Should().BeTrue();
        reference.Name.Should().Be(Name);
        reference.UpdatedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void Create_Should_ExposeEventData_When_ApproverReferenceIsBuilt()
    {
        ApproverId id = ApproverId.New();
        CustomerId customerId = CustomerId.New();

        ApproverReference reference = ApproverReference.Create(id, customerId, Name, Email, ReferenceStatus.Active, CreatedAt);

        reference.Id.Should().Be(id);
        reference.CustomerId.Should().Be(customerId);
        reference.FullName.Should().Be(Name);
        reference.Email.Should().Be(Email);
        reference.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Apply_Should_BeIdempotent_When_ApproverStatusIsAppliedTwice()
    {
        ApproverReference reference = ApproverReference.Create(ApproverId.New(), CustomerId.New(), Name, Email, ReferenceStatus.Active, CreatedAt);

        reference.Apply(ReferenceStatus.Inactive, UpdatedAt);
        reference.Apply(ReferenceStatus.Inactive, UpdatedAt);

        reference.IsActive.Should().BeFalse();
        reference.FullName.Should().Be(Name);
        reference.UpdatedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void Apply_Should_UpdateAllColumns_When_ApproverRegistrationIsApplied()
    {
        CustomerId newCustomerId = CustomerId.New();
        ApproverReference reference = ApproverReference.Create(ApproverId.New(), CustomerId.New(), Name, Email, ReferenceStatus.Inactive, CreatedAt);

        reference.Apply(newCustomerId, NewName, NewEmail, ReferenceStatus.Active, UpdatedAt);

        reference.CustomerId.Should().Be(newCustomerId);
        reference.FullName.Should().Be(NewName);
        reference.Email.Should().Be(NewEmail);
        reference.IsActive.Should().BeTrue();
    }
}
