namespace Auction.BLL.Constants;

internal static class MonetaryLimits
{
    public const decimal MaxAmount = 9999999999999999.99m;
    public const string MaxAmountString = "9999999999999999.99";
    public const string MinAmountString = "0.01";
    public const int MaxDecimalPlaces = 2;

    // Amounts are persisted as numeric(18,2); anything more precise would be
    // silently rounded by the database and leak fractions of a cent between
    // deductions, refunds and settlement.
    public static bool HasValidScale(decimal value) =>
        decimal.Round(value, MaxDecimalPlaces) == value;
}
