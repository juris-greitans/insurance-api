namespace Insurance.Api.Domain.Common;

/// <summary>
/// Rules for monetary amounts. Single implicit currency (EUR), so amounts are plain <see cref="decimal"/> values.
/// </summary>
public static class Money
{
    public const int MaxDecimalPlaces = 2;

    public static bool IsValidPositiveAmount(decimal amount) =>
        amount > 0 && decimal.Round(amount, MaxDecimalPlaces) == amount;
}
