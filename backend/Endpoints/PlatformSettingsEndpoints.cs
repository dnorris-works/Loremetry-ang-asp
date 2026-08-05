using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class PlatformSettingsEndpoints
{
    public static IEndpointRouteBuilder MapPlatformSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var platform = app.MapGroup("/api/admin/platform-settings").RequireOperator();

        platform.MapGet("/", GetPlatformSettings);
        platform.MapPut("/", UpdatePlatformSettings);
        platform.MapPost("/test", TestPlatformSettings);

        return app;
    }

    private static async Task<IResult> GetPlatformSettings(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await PlatformSettingsService.GetWithServiceStatusesAsync(db, cancellationToken);
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdatePlatformSettings(
        UpdatePlatformSettingsRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await PlatformSettingsService.UpdateAsync(request, db, cancellationToken);
            return Results.Ok(settings);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> TestPlatformSettings(
        TestPlatformSettingsRequest request,
        HttpRequest httpRequest,
        AuthService authService,
        PlatformConnectionTests connectionTests,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthEndpointHelpers.TryResolveUserAsync(httpRequest, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        var results = await connectionTests.TestAllAsync(request, cancellationToken);
        var currentSettings = await PlatformSettingsService.GetAsync(db, cancellationToken);
        var settingsForStatus = currentSettings with
        {
            AnthropicApiKey = request.AnthropicApiKey,
            TokenmixApiKey = request.TokenmixApiKey,
            CanopyApiKey = request.CanopyApiKey,
            DataForSeoLogin = request.DataForSeoLogin,
            DataForSeoPassword = request.DataForSeoPassword,
            DefaultProvider = request.DefaultProvider,
            DefaultModel = request.DefaultModel,
            CanopyPricingPlan = request.CanopyPricingPlan,
            AnthropicConfigured = !string.IsNullOrWhiteSpace(request.AnthropicApiKey),
            TokenmixConfigured = !string.IsNullOrWhiteSpace(request.TokenmixApiKey),
            CanopyConfigured = !string.IsNullOrWhiteSpace(request.CanopyApiKey),
            DataForSeoConfigured = !string.IsNullOrWhiteSpace(request.DataForSeoLogin)
                && !string.IsNullOrWhiteSpace(request.DataForSeoPassword),
        };

        await PlatformServiceStatusService.PersistConnectionTestResultsAsync(
            db,
            settingsForStatus,
            results,
            cancellationToken);

        var canopyResult = results.Results.FirstOrDefault(item => item.Service == "canopy");
        if (canopyResult is { Success: true, Configured: true })
        {
            await CanopyCostService.RecordUsageAsync(
                db,
                authResult.User!.DbUserId,
                "connection_test",
                CanopyPricingCatalog.Operations.ConnectionTest,
                "/api/amazon/autocomplete",
                cancellationToken);
        }

        return Results.Ok(results);
    }
}
