using backend.Configuration;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class CanopyCostService
{
    public const string Provider = "canopy";
    public const string UsageKind = "canopy_api";

    public static async Task<IReadOnlyList<CanopyPricingPlanDto>> ListPlansAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var plans = await db.CanopyPricingPlans
            .AsNoTracking()
            .OrderBy(plan => plan.SortOrder)
            .ThenBy(plan => plan.DisplayName)
            .ToListAsync(cancellationToken);

        return plans.Select(ToDto).ToList();
    }

    public static async Task<CanopyPricingPlan?> GetPlanAsync(
        AppDbContext db,
        string planId,
        CancellationToken cancellationToken)
    {
        var normalized = planId.Trim().ToLowerInvariant();
        return await db.CanopyPricingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(plan => plan.Id == normalized, cancellationToken);
    }

    public static async Task<string> ResolvePlanIdAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var setting = await db.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.SettingKey == PlatformSettingKeys.CanopyPricingPlan,
                cancellationToken);

        var planId = setting?.Value.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(planId))
        {
            var exists = await db.CanopyPricingPlans
                .AsNoTracking()
                .AnyAsync(plan => plan.Id == planId, cancellationToken);
            if (exists)
            {
                return planId;
            }
        }

        return PlatformSettingKeys.DefaultCanopyPricingPlan;
    }

    public static CanopyCostEstimateDto EstimateCost(
        CanopyPricingPlan plan,
        int requestCount,
        int requestsUsedThisMonth)
    {
        if (requestCount < 0 || requestsUsedThisMonth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestCount), "Request counts cannot be negative.");
        }

        var overageBefore = Math.Max(0, requestsUsedThisMonth - plan.MonthlyRequestAllowance);
        var overageAfter = Math.Max(0, requestsUsedThisMonth + requestCount - plan.MonthlyRequestAllowance);
        var billableRequests = overageAfter - overageBefore;
        var hardLimitReached = plan.OveragePricePerRequest <= 0
            && requestsUsedThisMonth + requestCount > plan.MonthlyRequestAllowance;

        var marginalCost = Math.Round(billableRequests * plan.OveragePricePerRequest, 6);

        return new CanopyCostEstimateDto(
            plan.Id,
            plan.DisplayName,
            requestCount,
            requestsUsedThisMonth,
            billableRequests,
            plan.MonthlyFeeUsd,
            marginalCost,
            hardLimitReached,
            PricingMissing: false);
    }

    public static async Task<CanopyCostEstimateDto> EstimateCostAsync(
        AppDbContext db,
        int requestCount,
        int requestsUsedThisMonth,
        string? planId,
        CancellationToken cancellationToken)
    {
        var resolvedPlanId = string.IsNullOrWhiteSpace(planId)
            ? await ResolvePlanIdAsync(db, cancellationToken)
            : planId.Trim().ToLowerInvariant();

        var plan = await GetPlanAsync(db, resolvedPlanId, cancellationToken);
        if (plan is null)
        {
            return new CanopyCostEstimateDto(
                resolvedPlanId,
                resolvedPlanId,
                requestCount,
                requestsUsedThisMonth,
                0,
                0,
                0,
                HardLimitReached: false,
                PricingMissing: true);
        }

        return EstimateCost(plan, requestCount, requestsUsedThisMonth);
    }

    public static async Task<IReadOnlyList<CanopyOperationEstimateDto>> EstimateKnownOperationsAsync(
        AppDbContext db,
        int requestsUsedThisMonth,
        string? planId,
        CancellationToken cancellationToken)
    {
        var operations = new (string Id, string Label, int Requests)[]
        {
            ("connection_test", "Connection test", CanopyPricingCatalog.Operations.ConnectionTest),
            ("category_stats", "Category stats (per path)", CanopyPricingCatalog.Operations.CategoryStats),
            ("keyword_search", "Keyword search", CanopyPricingCatalog.Operations.KeywordSearch),
        };

        var estimates = new List<CanopyOperationEstimateDto>(operations.Length);
        var used = requestsUsedThisMonth;

        foreach (var (id, label, requests) in operations)
        {
            var estimate = await EstimateCostAsync(db, requests, used, planId, cancellationToken);
            estimates.Add(new CanopyOperationEstimateDto(id, label, requests, estimate));
            used += requests;
        }

        return estimates;
    }

    public static async Task<AiUsageEvent> RecordUsageAsync(
        AppDbContext db,
        long userId,
        string feature,
        int requestCount,
        string? endpoint,
        CancellationToken cancellationToken)
    {
        var planId = await ResolvePlanIdAsync(db, cancellationToken);
        var plan = await GetPlanAsync(db, planId, cancellationToken);
        var usedThisMonth = await CountPlatformRequestsThisMonthAsync(db, cancellationToken);

        var estimate = plan is null
            ? new CanopyCostEstimateDto(
                planId,
                planId,
                requestCount,
                usedThisMonth,
                0,
                0,
                0,
                HardLimitReached: false,
                PricingMissing: true)
            : EstimateCost(plan, requestCount, usedThisMonth);

        var metadata = endpoint is null
            ? null
            : $$"""{"endpoint":"{{endpoint}}","billableRequests":{{estimate.BillableRequests}}}""";

        var usageEvent = new AiUsageEvent
        {
            UserId = userId,
            Kind = UsageKind,
            Provider = Provider,
            Model = planId,
            Feature = feature,
            InputTokens = requestCount,
            OutputTokens = 0,
            CostUsd = estimate.MarginalCostUsd,
            MetadataJson = metadata,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.AiUsageEvents.Add(usageEvent);
        await db.SaveChangesAsync(cancellationToken);

        return usageEvent;
    }

    public static async Task<int> CountPlatformRequestsThisMonthAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        return await db.AiUsageEvents
            .AsNoTracking()
            .Where(item =>
                item.Kind == UsageKind
                && item.Provider == Provider
                && item.CreatedAt >= monthStart)
            .SumAsync(item => item.InputTokens, cancellationToken);
    }

    private static CanopyPricingPlanDto ToDto(CanopyPricingPlan plan) =>
        new(
            plan.Id,
            plan.DisplayName,
            plan.MonthlyFeeUsd,
            plan.MonthlyRequestAllowance,
            plan.OveragePricePerRequest,
            plan.SortOrder,
            plan.SyncedAt);
}
