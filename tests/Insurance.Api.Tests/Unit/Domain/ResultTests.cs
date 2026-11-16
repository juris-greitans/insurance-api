using Insurance.Api.Domain.Common;

namespace Insurance.Api.Tests.Unit.Domain;

public class ResultTests
{
    private static readonly DomainError SampleError = DomainError.Conflict("Sample.Code", "Sample message.");

    [Fact]
    public void Success_WhenCalled_ThenIsSuccessWithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_WhenCalledWithError_ThenIsFailureWithThatError()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(SampleError, result.Error);
    }

    [Fact]
    public void Failure_WhenErrorIsNull_ThenThrows()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void op_Implicit_WhenFromError_ThenIsFailure()
    {
        Result result = SampleError;
        Result<int> typed = SampleError;

        Assert.Same(SampleError, result.Error);
        Assert.Same(SampleError, typed.Error);
    }

    [Fact]
    public void Value_WhenSuccess_ThenReturnsValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_WhenFailure_ThenThrowsWithErrorCode()
    {
        var result = Result.Failure<int>(SampleError);

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains(SampleError.Code, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Success_WhenValueIsNull_ThenThrows()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
    }

    [Fact]
    public void op_Implicit_WhenFromNullValue_ThenThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> result = (string)null!;
            return result;
        });
    }

    [Fact]
    public void Validation_WhenCalled_ThenCreatesValidationError()
    {
        AssertError(DomainError.Validation("C", "M"), ErrorType.Validation);
    }

    [Fact]
    public void NotFound_WhenCalled_ThenCreatesNotFoundError()
    {
        AssertError(DomainError.NotFound("C", "M"), ErrorType.NotFound);
    }

    [Fact]
    public void Conflict_WhenCalled_ThenCreatesConflictError()
    {
        AssertError(DomainError.Conflict("C", "M"), ErrorType.Conflict);
    }

    private static void AssertError(DomainError error, ErrorType expectedType)
    {
        Assert.Equal(expectedType, error.Type);
        Assert.Equal("C", error.Code);
        Assert.Equal("M", error.Message);
    }
}
