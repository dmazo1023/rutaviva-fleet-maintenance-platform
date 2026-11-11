using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Domain;

public sealed class ResultTests
{
    private const string Code = "Test.Failure";
    private const string Message = "Something failed.";
    private const int Value = 42;

    private static readonly Error TestError = new(Code, Message, ErrorType.Validation);

    [Fact]
    public void Success_Should_BeSuccessful_When_Created()
    {
        Result result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Error_Should_Throw_When_ResultIsSuccess()
    {
        Result result = Result.Success();

        Action act = () => _ = result.Error;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_Should_ExposeError_When_Created()
    {
        Result result = Result.Failure(TestError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TestError);
    }

    [Fact]
    public void Failure_Should_Throw_When_ErrorIsNull()
    {
        Action act = () => Result.Failure(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImplicitConversion_Should_ReturnSuccess_When_ConvertedFromValue()
    {
        Result<int> result = Value;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Value);
    }

    [Fact]
    public void ImplicitConversion_Should_ReturnFailure_When_ConvertedFromError()
    {
        Result<int> result = TestError;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TestError);
    }

    [Fact]
    public void Value_Should_Throw_When_ResultIsFailure()
    {
        Result<int> result = TestError;

        Action act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_Should_Throw_When_GenericResultIsSuccess()
    {
        Result<int> result = Value;

        Action act = () => _ = result.Error;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_Should_Throw_When_GenericErrorIsNull()
    {
        Action act = () => Result<int>.Failure(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
