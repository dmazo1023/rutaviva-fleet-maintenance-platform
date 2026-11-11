using RutaViva.Customers.Application.Approvers;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.UnitTests.Approvers;

public sealed class AddApproverCommandValidatorTests
{
    private const string EmptyText = "";
    private const string InvalidEmail = "not-an-email";

    private readonly AddApproverCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        AddApproverCommand command = new(CustomerId.New(), CustomerTestFactory.ApproverName, CustomerTestFactory.ApproverEmail);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_FullNameIsEmpty()
    {
        AddApproverCommand command = new(CustomerId.New(), EmptyText, CustomerTestFactory.ApproverEmail);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailHasInvalidFormat()
    {
        AddApproverCommand command = new(CustomerId.New(), CustomerTestFactory.ApproverName, InvalidEmail);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }
}
