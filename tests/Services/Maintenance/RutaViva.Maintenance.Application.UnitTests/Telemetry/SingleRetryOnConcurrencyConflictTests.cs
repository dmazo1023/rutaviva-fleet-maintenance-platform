using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Telemetry;

namespace RutaViva.Maintenance.Application.UnitTests.Telemetry;

public sealed class SingleRetryOnConcurrencyConflictTests
{
    private const string ConflictCode = "Concurrency.Conflict";
    private const string OtherCode = "Other.Failure";
    private const string Message = "Failure";
    private const int SuccessValue = 7;
    private const int OneAttempt = 1;
    private const int TwoAttempts = 2;

    private static readonly Error Conflict = new(ConflictCode, Message, ErrorType.Conflict);
    private static readonly Error Other = new(OtherCode, Message, ErrorType.BusinessRule);

    [Fact]
    public async Task ExecuteAsync_Should_RunOnce_When_FirstAttemptSucceeds()
    {
        ScriptedOperation operation = new(Result<int>.Success(SuccessValue));

        Result<int> result = await SingleRetryOnConcurrencyConflict.ExecuteAsync(operation.RunAsync, TestContext.Current.CancellationToken);

        result.Value.Should().Be(SuccessValue);
        operation.Attempts.Should().Be(OneAttempt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_ConflictIsFollowedBySuccess()
    {
        ScriptedOperation operation = new(Result<int>.Failure(Conflict), Result<int>.Success(SuccessValue));

        Result<int> result = await SingleRetryOnConcurrencyConflict.ExecuteAsync(operation.RunAsync, TestContext.Current.CancellationToken);

        result.Value.Should().Be(SuccessValue);
        operation.Attempts.Should().Be(TwoAttempts);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnConflict_When_SecondAttemptConflictsAgain()
    {
        ScriptedOperation operation = new(Result<int>.Failure(Conflict), Result<int>.Failure(Conflict), Result<int>.Success(SuccessValue));

        Result<int> result = await SingleRetryOnConcurrencyConflict.ExecuteAsync(operation.RunAsync, TestContext.Current.CancellationToken);

        result.Error.Code.Should().Be(ConflictCode);
        operation.Attempts.Should().Be(TwoAttempts);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotRetry_When_FailureIsNotAConflict()
    {
        ScriptedOperation operation = new(Result<int>.Failure(Other), Result<int>.Success(SuccessValue));

        Result<int> result = await SingleRetryOnConcurrencyConflict.ExecuteAsync(operation.RunAsync, TestContext.Current.CancellationToken);

        result.Error.Code.Should().Be(OtherCode);
        operation.Attempts.Should().Be(OneAttempt);
    }

    private sealed class ScriptedOperation(params Result<int>[] results)
    {
        private readonly Queue<Result<int>> _results = new(results);

        public int Attempts { get; private set; }

        public Task<Result<int>> RunAsync(CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromResult(_results.Dequeue());
        }
    }
}
