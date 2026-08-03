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
        var settings = await PlatformSettingsService.GetAsync(db, cancellationToken);
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
        PlatformConnectionTests connectionTests,
        CancellationToken cancellationToken)
    {
        var results = await connectionTests.TestAllAsync(request, cancellationToken);
        return Results.Ok(results);
    }
}
