using System.Globalization;

namespace Insurance.Api.Tests.Unit.Domain;

internal static class Parse
{
    public static DateOnly Date(string isoDate) =>
        DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static decimal Amount(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);
}
