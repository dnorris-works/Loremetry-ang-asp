using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class ProviderModelEndpoints
{
    public static IEndpointRouteBuilder MapProviderModelEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin/provider-models").RequireOperator();

        admin.MapGet("/", ListProviderModels);
        admin.MapPost("/sync-tokenmix", SyncTokenMixPricing);

        return app;
    }

    private static async Task<IResult> ListProviderModels(
        string? provider,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var models = await ProviderModelPricingService.ListAsync(
            db,
            provider ?? "tokenmix",
            cancellationToken);

        return Results.Ok(models);
    }

    private static async Task<IResult> SyncTokenMixPricing(
        TokenMixPricingSyncService syncService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var result = await syncService.SyncChatModelsAsync(db, cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
}
