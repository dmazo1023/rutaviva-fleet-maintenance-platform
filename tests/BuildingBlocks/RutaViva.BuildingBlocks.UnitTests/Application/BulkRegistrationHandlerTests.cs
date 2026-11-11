using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class BulkRegistrationHandlerTests
{
    private const string ExistingCode = "EXISTING";
    private const string NewCode = "NEW";
    private const string OtherNewCode = "OTHER";
    private const int FirstRow = 1;
    private const int SecondRow = 2;

    private readonly IUnitOfWork _realUnitOfWork = Substitute.For<IUnitOfWork>();

    public BulkRegistrationHandlerTests()
    {
        _realUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    private TestBulkRegistrationHandler CreateHandler(bool dryRun, Error? resolveError = null)
    {
        IUnitOfWork unitOfWork = BulkExecutionScope.For(dryRun).Resolve(_realUnitOfWork);
        SavingBulkCommandHandler commandHandler = new(unitOfWork);
        return new TestBulkRegistrationHandler(commandHandler, new HashSet<string> { ExistingCode }, resolveError);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnSkipped_When_NaturalKeyAlreadyExists()
    {
        TestBulkRegistrationHandler handler = CreateHandler(dryRun: false);

        IReadOnlyList<BulkRowResult> results = await handler.ExecuteAsync([new BulkRow(FirstRow, ExistingCode)], TestContext.Current.CancellationToken);

        results.Should().ContainSingle().Which.Outcome.Should().Be(BulkRowOutcome.Skipped);
        await _realUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidDuplicateInBatch_When_NaturalKeyRepeatsInBatch()
    {
        TestBulkRegistrationHandler handler = CreateHandler(dryRun: false);

        IReadOnlyList<BulkRowResult> results = await handler.ExecuteAsync(
            [new BulkRow(FirstRow, NewCode), new BulkRow(SecondRow, NewCode)],
            TestContext.Current.CancellationToken);

        results[0].Outcome.Should().Be(BulkRowOutcome.Valid);
        results[1].Outcome.Should().Be(BulkRowOutcome.Invalid);
        results[1].RowNumber.Should().Be(SecondRow);
        results[1].Errors.Should().ContainSingle().Which.Code.Should().Be(BulkErrorCodes.DuplicateInBatch);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NeverCallRealSaveChanges_When_DryRunIsTrue()
    {
        TestBulkRegistrationHandler handler = CreateHandler(dryRun: true);

        IReadOnlyList<BulkRowResult> results = await handler.ExecuteAsync(
            [new BulkRow(FirstRow, NewCode), new BulkRow(SecondRow, OtherNewCode)],
            TestContext.Current.CancellationToken);

        results.Should().OnlyContain(result => result.Outcome == BulkRowOutcome.Valid);
        await _realUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_SaveEachRowSeparately_When_DryRunIsFalse()
    {
        TestBulkRegistrationHandler handler = CreateHandler(dryRun: false);

        IReadOnlyList<BulkRowResult> results = await handler.ExecuteAsync(
            [new BulkRow(FirstRow, NewCode), new BulkRow(SecondRow, OtherNewCode)],
            TestContext.Current.CancellationToken);

        results.Should().OnlyContain(result => result.Outcome == BulkRowOutcome.Valid);
        await _realUnitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidWithResolveError_When_CommandCannotBeResolved()
    {
        Error error = new(BulkErrorCodes.VehicleOwnerNotFound, "Owner not found.", ErrorType.NotFound);
        TestBulkRegistrationHandler handler = CreateHandler(dryRun: false, error);

        IReadOnlyList<BulkRowResult> results = await handler.ExecuteAsync([new BulkRow(FirstRow, NewCode)], TestContext.Current.CancellationToken);

        results.Should().ContainSingle().Which.Errors.Should().ContainSingle().Which.Code.Should().Be(error.Code);
        await _realUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidWithCommandError_When_CommandHandlerFails()
    {
        Error error = new("Sample.Conflict", "Conflict.", ErrorType.Conflict);
        _realUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(error));
        TestBulkRegistrationHandler handler = CreateHandler(dryRun: false);

        IReadOnlyList<BulkRowResult> results = await handler.ExecuteAsync([new BulkRow(FirstRow, NewCode)], TestContext.Current.CancellationToken);

        results.Should().ContainSingle().Which.Errors.Should().ContainSingle().Which.Code.Should().Be(error.Code);
    }
}
