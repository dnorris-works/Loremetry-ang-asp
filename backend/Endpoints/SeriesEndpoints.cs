using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class SeriesEndpoints
{
    public static IEndpointRouteBuilder MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var series = app.MapGroup("/api/series");

        series.MapGet("/", ListSeries);
        series.MapGet("/{id:long}", GetSeries);
        series.MapPost("/", CreateSeries);
        series.MapPut("/{id:long}", UpdateSeries);

        return app;
    }

    private static async Task<IResult> ListSeries(
        HttpRequest request,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthEndpointHelpers.TryResolveUserAsync(request, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        var items = await SeriesService.ListForUserAsync(authResult.User!.DbUserId, db, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetSeries(
        long id,
        HttpRequest request,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthEndpointHelpers.TryResolveUserAsync(request, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        var item = await SeriesService.GetForUserAsync(authResult.User!.DbUserId, id, db, cancellationToken);
        return item is null
            ? Results.NotFound(new { message = $"Series '{id}' was not found." })
            : Results.Ok(item);
    }

    private static async Task<IResult> CreateSeries(
        CreateSeriesRequest request,
        HttpRequest httpRequest,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthEndpointHelpers.TryResolveUserAsync(httpRequest, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        try
        {
            var item = await SeriesService.CreateAsync(authResult.User!.DbUserId, request, db, cancellationToken);
            return Results.Created($"/api/series/{item.Id}", item);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> UpdateSeries(
        long id,
        UpdateSeriesRequest request,
        HttpRequest httpRequest,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthEndpointHelpers.TryResolveUserAsync(httpRequest, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        try
        {
            var item = await SeriesService.UpdateAsync(
                authResult.User!.DbUserId,
                id,
                request,
                db,
                cancellationToken);

            return item is null
                ? Results.NotFound(new { message = $"Series '{id}' was not found." })
                : Results.Ok(item);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }
}
