namespace backend.Models;

public class CanopyPricingPlan
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public double MonthlyFeeUsd { get; set; }

    public int MonthlyRequestAllowance { get; set; }

    public double OveragePricePerRequest { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset SyncedAt { get; set; }
}
