using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class PlatformSettingsEndpoints
{
    public static IEndpointRouteBuilder MapPlatformSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var platform = app.MapGroup("/api/admin/platform-settings");

        platform.MapGet("/", GetPlatformSettings);
        platform.MapPut("/", UpdatePlatformSettings);
        platform.MapPost("/test", TestPlatformSettings);

        return app;
    }

    private static async Task<IResult> GetPlatformSettings(
        HttpRequest request,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (await RequireOperatorAsync(request, authService, cancellationToken) is { } error)
        {
            return error;
        }

        var settings = await PlatformSettingsService.GetAsync(db, cancellationToken);
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdatePlatformSettings(
        UpdatePlatformSettingsRequest request,
        HttpRequest httpRequest,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (await RequireOperatorAsync(httpRequest, authService, cancellationToken) is { } error)
        {
            return error;
        }

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
        CancellationToken cancellationToken)
    {
        if (await RequireOperatorAsync(httpRequest, authService, cancellationToken) is { } error)
        {
            return error;
        }

        var results = await connectionTests.TestAllAsync(request, cancellationToken);
        return Results.Ok(results);
    }

    private static async Task<IResult?> RequireOperatorAsync(
        HttpRequest request,
        AuthService authService,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await authService.ResolveAsync(request.Headers, cancellationToken);
            return user.IsAdmin
                ? null
                : Results.Json(new { message = "Operator access required." }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (AuthException exception)
        {
            return Results.Json(new { message = exception.Message }, statusCode: StatusCodes.Status401Unauthorized);
        }
    }
}
