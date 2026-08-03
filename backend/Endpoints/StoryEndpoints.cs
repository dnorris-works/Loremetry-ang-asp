using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class StoryEndpoints
{
    public static IEndpointRouteBuilder MapStoryEndpoints(this IEndpointRouteBuilder app)
    {
        var stories = app.MapGroup("/api/stories");

        stories.MapGet("/", ListStories);
        stories.MapGet("/{id:long}", GetStory);
        stories.MapPost("/", CreateStory);
        stories.MapPut("/{id:long}", UpdateStory);

        return app;
    }

    private static async Task<IResult> ListStories(
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

        var stories = await StoryService.ListForUserAsync(authResult.User!.DbUserId, db, cancellationToken);
        return Results.Ok(stories);
    }

    private static async Task<IResult> GetStory(
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

        var story = await StoryService.GetForUserAsync(authResult.User!.DbUserId, id, db, cancellationToken);
        return story is null
            ? Results.NotFound(new { message = $"Story '{id}' was not found." })
            : Results.Ok(story);
    }

    private static async Task<IResult> CreateStory(
        CreateStoryRequest request,
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
            var story = await StoryService.CreateAsync(authResult.User!.DbUserId, request, db, cancellationToken);
            return Results.Created($"/api/stories/{story.Id}", story);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> UpdateStory(
        long id,
        UpdateStoryRequest request,
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
            var story = await StoryService.UpdateAsync(
                authResult.User!.DbUserId,
                id,
                request,
                db,
                cancellationToken);

            return story is null
                ? Results.NotFound(new { message = $"Story '{id}' was not found." })
                : Results.Ok(story);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }
}
