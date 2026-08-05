using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class WritingDraftEndpoints
{
    public static IEndpointRouteBuilder MapWritingDraftEndpoints(this IEndpointRouteBuilder app)
    {
        var drafts = app.MapGroup("/api/writing/drafts");

        drafts.MapPut("/", UpsertDraft);
        drafts.MapGet("/", GetDraft);
        drafts.MapDelete("/", DeleteDraft);

        return app;
    }

    private static async Task<IResult> UpsertDraft(
        UpsertWritingDraftRequest request,
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
            var draft = await WritingDraftService.UpsertAsync(
                authResult.User!.DbUserId,
                request,
                db,
                cancellationToken);

            return Results.Ok(draft);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> GetDraft(
        string draftKey,
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

        var draft = await WritingDraftService.GetAsync(
            authResult.User!.DbUserId,
            draftKey,
            db,
            cancellationToken);

        return draft is null ? Results.NotFound() : Results.Ok(draft);
    }

    private static async Task<IResult> DeleteDraft(
        string draftKey,
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

        var removed = await WritingDraftService.DeleteAsync(
            authResult.User!.DbUserId,
            draftKey,
            db,
            cancellationToken);

        return removed ? Results.NoContent() : Results.NotFound();
    }
}
