using backend.Auth;
using backend.Data;
using backend.Services;

namespace backend.Endpoints;

public static class DocumentTypeEndpoints
{
    public static IEndpointRouteBuilder MapDocumentTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var documentTypes = app.MapGroup("/api/document-types");

        documentTypes.MapGet("/", ListDocumentTypes);

        return app;
    }

    private static async Task<IResult> ListDocumentTypes(
        string? parent,
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

        if (parent is not null && parent is not "story" and not "series")
        {
            return Results.BadRequest(new { message = "Parent must be 'story' or 'series' when provided." });
        }

        var types = await DocumentTypeService.ListActiveAsync(db, parent, cancellationToken);
        return Results.Ok(types);
    }
}
