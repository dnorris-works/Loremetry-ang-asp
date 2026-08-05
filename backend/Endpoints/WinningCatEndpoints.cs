using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Services;

namespace backend.Endpoints;

public static class WinningCatEndpoints
{
    public static IEndpointRouteBuilder MapWinningCatEndpoints(this IEndpointRouteBuilder app)
    {
        var winningcat = app.MapGroup("/api/admin/winningcat").RequireOperator();

        winningcat.MapGet("/status", GetCatalogStatus);
        winningcat.MapPost("/upload", UploadCsv);
        winningcat.MapPost("/remove-stale", RemoveStale);

        return app;
    }

    private static async Task<IResult> GetCatalogStatus(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var status = await WinningCatImportService.GetCatalogStatusAsync(db, cancellationToken);
        return Results.Ok(status);
    }

    private static async Task<IResult> UploadCsv(
        HttpRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "Expected multipart form data with a CSV file." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "No file uploaded." });
        }

        string csvText;
        await using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream))
        {
            csvText = await reader.ReadToEndAsync(cancellationToken);
        }

        var result = await WinningCatImportService.ImportCsvAsync(db, csvText, cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> RemoveStale(
        WinningCatRemoveStaleRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var result = await WinningCatImportService.RemoveStaleAsync(db, request.Since, cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
}
