using Insurance.Api.Domain.Common;

namespace Insurance.Api.Domain.Customers;

public sealed record Customer
{
    public const int FullNameMaxLength = 250;

    private Customer(Guid id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public Guid Id { get; }

    public string FullName { get; }

    public static Result<Customer> Create(string? fullName)
    {
        var trimmed = fullName?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return CustomerErrors.FullNameRequired;
        }

        if (trimmed.Length > FullNameMaxLength)
        {
            return CustomerErrors.FullNameTooLong;
        }

        return new Customer(Guid.NewGuid(), trimmed);
    }
}
