using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.Events;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Domain.UnitTests;

public sealed class WorkshopTests
{
    private const string WorkshopName = "Autofrenos Medellín";
    private const string PaddedName = "  Autofrenos Medellín  ";
    private const string TaxIdValue = "900100001";
    private const string EmailValue = "contacto@autofrenos.example";
    private const string PhoneValue = "6044440001";
    private const string AddressValue = "Cra 50 # 10-20";
    private const string CityValue = "Medellín";
    private const string Blank = "   ";
    private const char Filler = 'x';
    private const int SingleEvent = 1;
    private const int NoEvents = 0;
    private const int TwoEvents = 2;

    [Fact]
    public void Register_Should_CreateActiveWorkshop_When_DataIsValid()
    {
        Result<Workshop> result = Workshop.Register(WorkshopName, BuildTaxId(), BuildEmail(), BuildPhone(), BuildAddress());

        result.IsSuccess.Should().BeTrue();
        Workshop workshop = result.Value;
        workshop.Id.Value.Should().NotBe(Guid.Empty);
        workshop.Name.Should().Be(WorkshopName);
        workshop.TaxId.Value.Should().Be(TaxIdValue);
        workshop.Email.Value.Should().Be(EmailValue);
        workshop.Phone!.Value.Should().Be(PhoneValue);
        workshop.Address.Address.Should().Be(AddressValue);
        workshop.Address.City.Should().Be(CityValue);
        workshop.Status.Should().Be(WorkshopStatus.Active);
    }

    [Fact]
    public void Register_Should_AllowMissingPhone_When_PhoneIsNull()
    {
        Result<Workshop> result = Workshop.Register(WorkshopName, BuildTaxId(), BuildEmail(), null, BuildAddress());

        result.IsSuccess.Should().BeTrue();
        result.Value.Phone.Should().BeNull();
    }

    [Fact]
    public void Register_Should_TrimName_When_NameIsPadded()
    {
        Result<Workshop> result = Workshop.Register(PaddedName, BuildTaxId(), BuildEmail(), BuildPhone(), BuildAddress());

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(WorkshopName);
    }

    [Fact]
    public void Register_Should_RaiseWorkshopRegisteredWithData_When_DataIsValid()
    {
        Workshop workshop = BuildWorkshop();

        workshop.DomainEvents.Should().HaveCount(SingleEvent);
        WorkshopRegistered raised = workshop.DomainEvents.Single().Should().BeOfType<WorkshopRegistered>().Subject;
        raised.WorkshopId.Should().Be(workshop.Id);
        raised.TaxId.Value.Should().Be(TaxIdValue);
        raised.Name.Should().Be(WorkshopName);
        raised.City.Should().Be(CityValue);
        raised.Status.Should().Be(WorkshopStatus.Active);
        raised.OccurredAt.Should().Be(workshop.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    public void Register_Should_ReturnNameRequiredError_When_NameIsBlank(string? name)
    {
        Result<Workshop> result = Workshop.Register(name, BuildTaxId(), BuildEmail(), BuildPhone(), BuildAddress());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkshopErrors.NameRequired);
    }

    [Fact]
    public void Register_Should_ReturnNameTooLongError_When_NameExceedsMaxLength()
    {
        string name = new(Filler, Workshop.MaxNameLength + 1);

        Result<Workshop> result = Workshop.Register(name, BuildTaxId(), BuildEmail(), BuildPhone(), BuildAddress());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkshopErrors.NameTooLong);
    }

    [Fact]
    public void Deactivate_Should_ChangeStatusAndRaiseEvent_When_WorkshopIsActive()
    {
        Workshop workshop = BuildWorkshop();

        Result result = workshop.Deactivate();

        result.IsSuccess.Should().BeTrue();
        workshop.Status.Should().Be(WorkshopStatus.Inactive);
        workshop.DomainEvents.Should().HaveCount(TwoEvents);
        WorkshopStatusChanged raised = workshop.DomainEvents.Last().Should().BeOfType<WorkshopStatusChanged>().Subject;
        raised.WorkshopId.Should().Be(workshop.Id);
        raised.Status.Should().Be(WorkshopStatus.Inactive);
    }

    [Fact]
    public void Deactivate_Should_RaiseOnlyOneStatusEvent_When_CalledTwice()
    {
        Workshop workshop = BuildWorkshop();

        Result first = workshop.Deactivate();
        Result second = workshop.Deactivate();

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        workshop.DomainEvents.OfType<WorkshopStatusChanged>().Should().HaveCount(SingleEvent);
    }

    [Fact]
    public void Activate_Should_NotRaiseEvent_When_WorkshopIsAlreadyActive()
    {
        Workshop workshop = BuildWorkshop();

        Result result = workshop.Activate();

        result.IsSuccess.Should().BeTrue();
        workshop.Status.Should().Be(WorkshopStatus.Active);
        workshop.DomainEvents.OfType<WorkshopStatusChanged>().Should().HaveCount(NoEvents);
    }

    [Fact]
    public void Activate_Should_ChangeStatusAndRaiseEvent_When_WorkshopIsInactive()
    {
        Workshop workshop = BuildWorkshop();
        workshop.Deactivate();

        Result result = workshop.Activate();

        result.IsSuccess.Should().BeTrue();
        workshop.Status.Should().Be(WorkshopStatus.Active);
        WorkshopStatusChanged raised = workshop.DomainEvents.Last().Should().BeOfType<WorkshopStatusChanged>().Subject;
        raised.Status.Should().Be(WorkshopStatus.Active);
    }

    private static Workshop BuildWorkshop() =>
        Workshop.Register(WorkshopName, BuildTaxId(), BuildEmail(), BuildPhone(), BuildAddress()).Value;

    private static TaxId BuildTaxId() => TaxId.Create(TaxIdValue).Value;

    private static EmailAddress BuildEmail() => EmailAddress.Create(EmailValue).Value;

    private static PhoneNumber BuildPhone() => PhoneNumber.Create(PhoneValue).Value;

    private static PostalAddress BuildAddress() => PostalAddress.Create(AddressValue, CityValue).Value;
}
