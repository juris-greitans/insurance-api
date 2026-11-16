using Insurance.Api.Controllers;
using Insurance.Api.Domain.Common;

namespace Insurance.Api.Tests.Unit.Controllers;

public class ApiControllerBaseTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    public void StatusCodeFor_WhenKnownErrorType_ThenMapsToHttpStatus(ErrorType type, int expectedStatus)
    {
        Assert.Equal(expectedStatus, ApiControllerBase.StatusCodeFor(type));
    }

    [Fact]
    public void StatusCodeFor_WhenUnknownErrorType_ThenThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ApiControllerBase.StatusCodeFor((ErrorType)99));
    }
}
