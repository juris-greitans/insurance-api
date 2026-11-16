using Insurance.Api.Domain.Common;

namespace Insurance.Api.Tests.Unit.Domain.Common;

public class DomainErrorTests
{
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
