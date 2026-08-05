namespace backend.Services;

/// <summary>
/// Public Canopy API plan rates from https://www.canopyapi.co/ (refreshed by sync job).
/// </summary>
public static class CanopyPricingCatalog
{
    public sealed record PlanSeed(
        string Id,
        string DisplayName,
        double MonthlyFeeUsd,
        int MonthlyRequestAllowance,
        double OveragePricePerRequest,
        int SortOrder);

    public static IReadOnlyList<PlanSeed> Plans { get; } =
    [
        new("hobby", "Hobby", 0, 100, 0, 1),
        new("pay_as_you_go", "Pay As You Go", 0, 100, 0.01, 2),
        new("growth", "Growth ($99/mo)", 99, 20_000, 0.008, 3),
        new("premium", "Premium ($400/mo)", 400, 100_000, 0.004, 4),
    ];

    public static class Operations
    {
        public const int ConnectionTest = 1;

        /// <summary>
        /// One bestsellers + two sales + up to twenty product lookups (Loremetry category_stats).
        /// </summary>
        public const int CategoryStats = 23;

        public const int KeywordSearch = 1;
        public const int Autocomplete = 1;
    }
}
