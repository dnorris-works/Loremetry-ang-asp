using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class SeriesService
{
    public static async Task<IReadOnlyList<SeriesSummaryDto>> ListForUserAsync(
        long userId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Series
            .AsNoTracking()
            .Where(series => series.UserId == userId)
            .OrderByDescending(series => series.UpdatedAt)
            .Select(series => new SeriesSummaryDto(
                series.Id,
                series.Name,
                series.BibleDocuments.Count,
                series.CreatedAt,
                series.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public static async Task<SeriesDetailDto?> GetForUserAsync(
        long userId,
        long seriesId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var series = await db.Series
            .AsNoTracking()
            .Include(item => item.BibleDocuments.OrderBy(document => document.SortOrder))
            .FirstOrDefaultAsync(item => item.Id == seriesId && item.UserId == userId, cancellationToken);

        return series is null ? null : ToDetailDto(series);
    }

    public static async Task<SeriesSummaryDto> CreateAsync(
        long userId,
        CreateSeriesRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is { } message)
        {
            throw new InvalidOperationException(message);
        }

        var now = DateTimeOffset.UtcNow;
        var series = new Series
        {
            UserId = userId,
            Name = request.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var mappedDocuments = DocumentInputHelper.MapBibleDocuments(request.Bibles ?? []);
        foreach (var (document, index) in mappedDocuments.Select((document, index) => (document, index)))
        {
            series.BibleDocuments.Add(new SeriesBibleDocument
            {
                FileName = document.FileName,
                MimeType = document.MimeType,
                TextContent = document.TextContent,
                BinaryContent = document.BinaryContent,
                SortOrder = index,
                CreatedAt = now,
            });
        }

        db.Series.Add(series);
        await db.SaveChangesAsync(cancellationToken);

        return ToSummaryDto(series);
    }

    private static string? ValidateCreateRequest(CreateSeriesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Series name is required.";
        }

        if (request.Name.Trim().Length > 200)
        {
            return "Series name must be 200 characters or fewer.";
        }

        return DocumentInputHelper.ValidateBibleDocuments(request.Bibles ?? []);
    }

    private static SeriesSummaryDto ToSummaryDto(Series series) =>
        new(
            series.Id,
            series.Name,
            series.BibleDocuments.Count,
            series.CreatedAt,
            series.UpdatedAt);

    private static SeriesDetailDto ToDetailDto(Series series) =>
        new(
            series.Id,
            series.Name,
            series.BibleDocuments
                .OrderBy(document => document.SortOrder)
                .Select(document => new SeriesBibleDocumentDto(
                    document.Id,
                    document.FileName,
                    document.MimeType,
                    !string.IsNullOrEmpty(document.TextContent),
                    document.BinaryContent is { Length: > 0 },
                    document.SortOrder))
                .ToList(),
            series.CreatedAt,
            series.UpdatedAt);
}
