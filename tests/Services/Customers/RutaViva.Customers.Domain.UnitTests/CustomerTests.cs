using System.Globalization;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.Events;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain.UnitTests;

public sealed class CustomerTests
{
    private const string CustomerNameText = "RutaViva S.A.S.";
    private const string TaxIdText = "900000001";
    private const string CustomerEmailText = "flota@rutaviva.example";
    private const string PhoneText = "6045550001";
    private const string ApproverNameText = "Laura Gómez";
    private const string ApproverEmailText = "Laura_Gomez@rutaviva.com";
    private const string ApproverEmailUpperText = "LAURA_GOMEZ@RUTAVIVA.COM";
    private const string OtherApproverNameText = "Andrés Restrepo";
    private const string OtherApproverEmailText = "Andres_Restrepo@rutaviva.com";
    private const string RegisteredAtText = "2026-11-01T08:00:00-05:00";
    private const string ChangedAtText = "2026-11-02T09:30:00-05:00";
    private const string LaterChangedAtText = "2026-11-03T10:45:00-05:00";
    private const string EmailConflictCode = "Customer.ApproverEmailAlreadyRegistered";
    private const string ApproverNotFoundCode = "Customer.ApproverNotFound";
    private const string InactiveCode = "Customer.Inactive";
    private const int TwoApprovers = 2;
    private const int TwoStatusEvents = 2;

    private static readonly DateTimeOffset RegisteredAt = Parse(RegisteredAtText);
    private static readonly DateTimeOffset ChangedAt = Parse(ChangedAtText);
    private static readonly DateTimeOffset LaterChangedAt = Parse(LaterChangedAtText);

    [Fact]
    public void Register_Should_CreateActiveCustomerAndEmitCustomerRegistered_When_DataIsValid()
    {
        Customer customer = RegisterCustomer();

        customer.Status.Should().Be(CustomerStatus.Active);
        customer.Name.Value.Should().Be(CustomerNameText);
        customer.TaxId.Value.Should().Be(TaxIdText);
        customer.Email.Value.Should().Be(CustomerEmailText);
        customer.Phone!.Value.Should().Be(PhoneText);
        customer.Approvers.Should().BeEmpty();
        CustomerRegistered registered = customer.DomainEvents.OfType<CustomerRegistered>().Single();
        customer.DomainEvents.Should().ContainSingle();
        registered.CustomerId.Should().Be(customer.Id);
        registered.TaxId.Should().Be(customer.TaxId);
        registered.Name.Should().Be(customer.Name);
        registered.Email.Should().Be(customer.Email);
        registered.Status.Should().Be(CustomerStatus.Active);
        registered.OccurredAt.Should().Be(RegisteredAt);
    }

    [Fact]
    public void Register_Should_KeepPhoneNull_When_PhoneIsNotProvided()
    {
        Customer customer = Customer.Register(Name(CustomerNameText), Tax(TaxIdText), Email(CustomerEmailText), null, RegisteredAt);

        customer.Phone.Should().BeNull();
    }

    [Fact]
    public void AddApprover_Should_AddActiveApproverAndEmitApproverRegistered_When_EmailIsNew()
    {
        Customer customer = RegisterCustomer();

        Result<ApproverId> result = customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), ChangedAt);

        result.IsSuccess.Should().BeTrue();
        Approver approver = customer.Approvers.Single();
        approver.Id.Should().Be(result.Value);
        approver.CustomerId.Should().Be(customer.Id);
        approver.FullName.Value.Should().Be(ApproverNameText);
        approver.Email.Value.Should().Be(ApproverEmailText.ToLowerInvariant());
        approver.Status.Should().Be(ApproverStatus.Active);
        ApproverRegistered registered = customer.DomainEvents.OfType<ApproverRegistered>().Single();
        registered.ApproverId.Should().Be(approver.Id);
        registered.CustomerId.Should().Be(customer.Id);
        registered.FullName.Should().Be(approver.FullName);
        registered.Email.Should().Be(approver.Email);
        registered.Status.Should().Be(ApproverStatus.Active);
        registered.OccurredAt.Should().Be(ChangedAt);
    }

    [Theory]
    [InlineData(ApproverEmailText)]
    [InlineData(ApproverEmailUpperText)]
    public void AddApprover_Should_ReturnEmailConflictAndEmitNothing_When_EmailAlreadyExistsInCustomer(string repeatedEmail)
    {
        Customer customer = RegisterCustomer();
        customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), ChangedAt);
        int eventsBefore = customer.DomainEvents.Count;

        Result<ApproverId> result = customer.AddApprover(Name(OtherApproverNameText), Email(repeatedEmail), LaterChangedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(EmailConflictCode);
        result.Error.Type.Should().Be(ErrorType.Conflict);
        customer.Approvers.Should().ContainSingle();
        customer.DomainEvents.Should().HaveCount(eventsBefore);
    }

    [Fact]
    public void AddApprover_Should_ReturnEmailConflict_When_ExistingApproverWithSameEmailIsInactive()
    {
        Customer customer = RegisterCustomer();
        ApproverId approverId = customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), ChangedAt).Value;
        customer.DeactivateApprover(approverId, ChangedAt);

        Result<ApproverId> result = customer.AddApprover(Name(OtherApproverNameText), Email(ApproverEmailText), LaterChangedAt);

        result.Error.Code.Should().Be(EmailConflictCode);
    }

    [Fact]
    public void AddApprover_Should_AcceptDifferentEmails_When_ApproversBelongToTheSameCustomer()
    {
        Customer customer = RegisterCustomer();

        customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), ChangedAt);
        Result<ApproverId> second = customer.AddApprover(Name(OtherApproverNameText), Email(OtherApproverEmailText), ChangedAt);

        second.IsSuccess.Should().BeTrue();
        customer.Approvers.Should().HaveCount(TwoApprovers);
    }

    [Fact]
    public void AddApprover_Should_ReturnInactiveError_When_CustomerIsInactive()
    {
        Customer customer = RegisterCustomer();
        customer.Deactivate(ChangedAt);

        Result<ApproverId> result = customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), LaterChangedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InactiveCode);
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
        customer.Approvers.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_Should_EmitSingleStatusChanged_When_CalledTwice()
    {
        Customer customer = RegisterCustomer();

        Result first = customer.Deactivate(ChangedAt);
        Result second = customer.Deactivate(LaterChangedAt);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Inactive);
        CustomerStatusChanged changed = customer.DomainEvents.OfType<CustomerStatusChanged>().Single();
        changed.CustomerId.Should().Be(customer.Id);
        changed.Status.Should().Be(CustomerStatus.Inactive);
        changed.OccurredAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void Activate_Should_SucceedWithoutEvent_When_CustomerIsAlreadyActive()
    {
        Customer customer = RegisterCustomer();

        Result result = customer.Activate(ChangedAt);

        result.IsSuccess.Should().BeTrue();
        customer.DomainEvents.OfType<CustomerStatusChanged>().Should().BeEmpty();
    }

    [Fact]
    public void Activate_Should_EmitStatusChanged_When_CustomerWasInactive()
    {
        Customer customer = RegisterCustomer();
        customer.Deactivate(ChangedAt);

        Result result = customer.Activate(LaterChangedAt);

        result.IsSuccess.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Active);
        customer.DomainEvents.OfType<CustomerStatusChanged>().Should().HaveCount(TwoStatusEvents);
        customer.DomainEvents.OfType<CustomerStatusChanged>().Last().Status.Should().Be(CustomerStatus.Active);
    }

    [Fact]
    public void DeactivateApprover_Should_EmitSingleStatusChanged_When_CalledTwice()
    {
        Customer customer = RegisterCustomer();
        ApproverId approverId = customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), ChangedAt).Value;

        Result first = customer.DeactivateApprover(approverId, ChangedAt);
        Result second = customer.DeactivateApprover(approverId, LaterChangedAt);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        customer.Approvers.Single().Status.Should().Be(ApproverStatus.Inactive);
        ApproverStatusChanged changed = customer.DomainEvents.OfType<ApproverStatusChanged>().Single();
        changed.ApproverId.Should().Be(approverId);
        changed.CustomerId.Should().Be(customer.Id);
        changed.Status.Should().Be(ApproverStatus.Inactive);
        changed.OccurredAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void ActivateApprover_Should_SucceedWithoutEvent_When_ApproverIsAlreadyActive()
    {
        Customer customer = RegisterCustomer();
        ApproverId approverId = customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), ChangedAt).Value;

        Result result = customer.ActivateApprover(approverId, LaterChangedAt);

        result.IsSuccess.Should().BeTrue();
        customer.DomainEvents.OfType<ApproverStatusChanged>().Should().BeEmpty();
    }

    [Fact]
    public void ActivateApprover_Should_EmitStatusChanged_When_ApproverWasInactive()
    {
        Customer customer = RegisterCustomer();
        ApproverId approverId = customer.AddApprover(Name(ApproverNameText), Email(ApproverEmailText), ChangedAt).Value;
        customer.DeactivateApprover(approverId, ChangedAt);

        Result result = customer.ActivateApprover(approverId, LaterChangedAt);

        result.IsSuccess.Should().BeTrue();
        customer.Approvers.Single().Status.Should().Be(ApproverStatus.Active);
        customer.DomainEvents.OfType<ApproverStatusChanged>().Last().Status.Should().Be(ApproverStatus.Active);
    }

    [Fact]
    public void DeactivateApprover_Should_ReturnApproverNotFound_When_ApproverDoesNotBelongToCustomer()
    {
        Customer customer = RegisterCustomer();

        Result result = customer.DeactivateApprover(ApproverId.New(), ChangedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ApproverNotFoundCode);
        result.Error.Type.Should().Be(ErrorType.NotFound);
        customer.DomainEvents.OfType<ApproverStatusChanged>().Should().BeEmpty();
    }

    [Fact]
    public void ActivateApprover_Should_ReturnApproverNotFound_When_ApproverDoesNotBelongToCustomer()
    {
        Customer customer = RegisterCustomer();

        Result result = customer.ActivateApprover(ApproverId.New(), ChangedAt);

        result.Error.Code.Should().Be(ApproverNotFoundCode);
    }

    private static Customer RegisterCustomer() =>
        Customer.Register(Name(CustomerNameText), Tax(TaxIdText), Email(CustomerEmailText), Phone(PhoneText), RegisteredAt);

    private static PersonName Name(string value) => PersonName.Create(value).Value;

    private static TaxId Tax(string value) => TaxId.Create(value).Value;

    private static EmailAddress Email(string value) => EmailAddress.Create(value).Value;

    private static PhoneNumber Phone(string value) => PhoneNumber.Create(value).Value;

    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
