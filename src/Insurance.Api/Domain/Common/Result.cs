namespace Insurance.Api.Domain.Common;

public class Result
{
    protected Result(DomainError? error)
    {
        Error = error;
    }

    public DomainError? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);

    public static Result<T> Success<T>(T value) =>
        new(value ?? throw new ArgumentNullException(nameof(value)), null);

    public static Result Failure(DomainError error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<T> Failure<T>(DomainError error) =>
        new(default, error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result(DomainError error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, DomainError? error)
        : base(error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error!.Code}).");

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(DomainError error) => Failure<T>(error);
}
