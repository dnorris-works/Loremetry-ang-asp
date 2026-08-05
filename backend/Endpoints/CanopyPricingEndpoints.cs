using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class CanopyPricingEndpoints
{
    public static IEndpointRouteBuilder MapCanopyPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin/canopy-pricing").RequireOperator();

        admin.MapGet("/plans", ListPlans);
        admin.MapPost("/sync", SyncPlans);
        admin.MapPost("/estimate", EstimateCost);
        admin.MapGet("/operations", EstimateOperations);

        return app;
    }

    private static async Task<IResult> ListPlans(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var plans = await CanopyCostService.ListPlansAsync(db, cancellationToken);
        var planId = await CanopyCostService.ResolvePlanIdAsync(db, cancellationToken);
        var usedThisMonth = await CanopyCostService.CountPlatformRequestsThisMonthAsync(db, cancellationToken);

        return Results.Ok(new
        {
            activePlanId = planId,
            requestsUsedThisMonth = usedThisMonth,
            plans,
        });
    }

    private static async Task<IResult> SyncPlans(
        CanopyPricingSyncService syncService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var result = await syncService.SyncPlansAsync(db, cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> EstimateCost(
        CanopyCostEstimateRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.RequestCount < 0 || request.RequestsUsedThisMonth < 0)
        {
            return Results.BadRequest(new { message = "Request counts cannot be negative." });
        }

        var estimate = await CanopyCostService.EstimateCostAsync(
            db,
            request.RequestCount,
            request.RequestsUsedThisMonth,
            request.PlanId,
            cancellationToken);

        return Results.Ok(estimate);
    }

    private static async Task<IResult> EstimateOperations(
        AppDbContext db,
        string? planId,
        int? requestsUsedThisMonth,
        CancellationToken cancellationToken)
    {
        var estimates = await CanopyCostService.EstimateKnownOperationsAsync(
            db,
            requestsUsedThisMonth ?? 0,
            planId,
            cancellationToken);

        return Results.Ok(estimates);
    }
}
