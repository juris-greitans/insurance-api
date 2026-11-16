using Insurance.Api.Domain.Common;

namespace Insurance.Api.Domain.Customers;

public static class CustomerErrors
{
    public static readonly DomainError FullNameRequired =
        DomainError.Validation("Customer.FullNameRequired", "Full name is required.");

    public static readonly DomainError FullNameTooLong = DomainError.Validation(
        "Customer.FullNameTooLong",
        $"Full name must be at most {Customer.FullNameMaxLength} characters.");
}
