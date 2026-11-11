using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.UnitTests.Customers;

public sealed class ChangeCustomerStatusCommandValidatorTests
{
    private const int UndefinedStatus = 0;

    private readonly ChangeCustomerStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(CustomerStatus.Active)]
    [InlineData(CustomerStatus.Inactive)]
    public void Validate_Should_Pass_When_StatusIsDefined(CustomerStatus status)
    {
        ChangeCustomerStatusCommand command = new(CustomerId.New(), status);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_StatusIsUndefined()
    {
        ChangeCustomerStatusCommand command = new(CustomerId.New(), (CustomerStatus)UndefinedStatus);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }
}
