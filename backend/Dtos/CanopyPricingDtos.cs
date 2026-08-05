namespace backend.Dtos;

public record CanopyPricingPlanDto(
    string Id,
    string DisplayName,
    double MonthlyFeeUsd,
    int MonthlyRequestAllowance,
    double OveragePricePerRequest,
    int SortOrder,
    DateTimeOffset SyncedAt);

public record CanopyPricingSyncResultDto(
    bool Success,
    int Plans,
    DateTimeOffset SyncedAt,
    string? Error);

public record CanopyCostEstimateRequest(
    int RequestCount,
    int RequestsUsedThisMonth = 0,
    string? PlanId = null);

public record CanopyCostEstimateDto(
    string PlanId,
    string PlanDisplayName,
    int RequestCount,
    int RequestsUsedThisMonth,
    int BillableRequests,
    double MonthlyFeeUsd,
    double MarginalCostUsd,
    bool HardLimitReached,
    bool PricingMissing);

public record CanopyOperationEstimateDto(
    string OperationId,
    string Label,
    int RequestCount,
    CanopyCostEstimateDto Estimate);
