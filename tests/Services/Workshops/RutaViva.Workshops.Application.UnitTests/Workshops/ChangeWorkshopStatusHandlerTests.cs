using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.Events;

namespace RutaViva.Workshops.Application.UnitTests.Workshops;

public sealed class ChangeWorkshopStatusHandlerTests
{
    private const int RegistrationEventCount = 1;
    private const int RegistrationAndChangeEventCount = 2;

    private readonly IWorkshopRepository _workshops = Substitute.For<IWorkshopRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ChangeWorkshopStatusHandler _handler;

    public ChangeWorkshopStatusHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new ChangeWorkshopStatusHandler(_workshops, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_DeactivateAndSave_When_TargetIsInactive()
    {
        Workshop workshop = StubWorkshop();
        ChangeWorkshopStatusCommand command = new(workshop.Id, WorkshopStatus.Inactive);

        Result<WorkshopId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(workshop.Id);
        workshop.Status.Should().Be(WorkshopStatus.Inactive);
        workshop.DomainEvents.Should().HaveCount(RegistrationAndChangeEventCount);
        workshop.DomainEvents.Last().Should().BeOfType<WorkshopStatusChanged>();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReactivateAndSave_When_TargetIsActive()
    {
        Workshop workshop = StubWorkshop();
        workshop.Deactivate();
        ChangeWorkshopStatusCommand command = new(workshop.Id, WorkshopStatus.Active);

        Result<WorkshopId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        workshop.Status.Should().Be(WorkshopStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_NotRaiseEvent_When_TargetEqualsCurrentStatus()
    {
        Workshop workshop = StubWorkshop();
        ChangeWorkshopStatusCommand command = new(workshop.Id, WorkshopStatus.Active);

        Result<WorkshopId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        workshop.DomainEvents.Should().HaveCount(RegistrationEventCount);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_WorkshopDoesNotExist()
    {
        ChangeWorkshopStatusCommand command = new(WorkshopId.New(), WorkshopStatus.Inactive);
        _workshops.GetByIdAsync(command.WorkshopId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Workshop?>(null));

        Result<WorkshopId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(WorkshopErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSaveError_When_SaveFails()
    {
        Workshop workshop = StubWorkshop();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(WorkshopErrors.NotFound)));

        Result<WorkshopId> result = await _handler.HandleAsync(
            new ChangeWorkshopStatusCommand(workshop.Id, WorkshopStatus.Inactive),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(WorkshopErrors.NotFound);
    }

    private Workshop StubWorkshop()
    {
        Workshop workshop = WorkshopTestFactory.ValidWorkshop();
        _workshops.GetByIdAsync(workshop.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Workshop?>(workshop));
        return workshop;
    }
}
