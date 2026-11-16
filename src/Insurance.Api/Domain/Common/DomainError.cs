namespace Insurance.Api.Domain.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

public sealed record DomainError(ErrorType Type, string Code, string Message)
{
    public static DomainError Validation(string code, string message) => new(ErrorType.Validation, code, message);

    public static DomainError NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static DomainError Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
}
