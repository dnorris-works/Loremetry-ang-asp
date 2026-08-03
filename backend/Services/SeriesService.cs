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
                series.BibleDocuments.Count(document => document.Category == BibleDocumentCategories.Character),
                series.BibleDocuments.Count(document => document.Category == BibleDocumentCategories.Location),
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
        var series = new LoreSeries
        {
            UserId = userId,
            Name = request.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var sortOrder = 0;
        foreach (var document in MapCategoryDocuments(request.Characters ?? [], BibleDocumentCategories.Character, now))
        {
            document.SortOrder = sortOrder++;
            series.BibleDocuments.Add(document);
        }

        foreach (var document in MapCategoryDocuments(request.Locations ?? [], BibleDocumentCategories.Location, now))
        {
            document.SortOrder = sortOrder++;
            series.BibleDocuments.Add(document);
        }

        db.Series.Add(series);
        await db.SaveChangesAsync(cancellationToken);

        return ToSummaryDto(series);
    }

    public static async Task<SeriesSummaryDto?> UpdateAsync(
        long userId,
        long seriesId,
        UpdateSeriesRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUpdateRequest(request);
        if (validationError is { } message)
        {
            throw new InvalidOperationException(message);
        }

        var series = await db.Series
            .Include(item => item.BibleDocuments)
            .FirstOrDefaultAsync(item => item.Id == seriesId && item.UserId == userId, cancellationToken);

        if (series is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        series.Name = request.Name.Trim();
        series.UpdatedAt = now;
        db.SeriesBibleDocuments.RemoveRange(series.BibleDocuments);
        series.BibleDocuments.Clear();

        var sortOrder = 0;
        foreach (var document in MapCategoryDocuments(request.Characters ?? [], BibleDocumentCategories.Character, now))
        {
            document.SeriesId = series.Id;
            document.SortOrder = sortOrder++;
            series.BibleDocuments.Add(document);
        }

        foreach (var document in MapCategoryDocuments(request.Locations ?? [], BibleDocumentCategories.Location, now))
        {
            document.SeriesId = series.Id;
            document.SortOrder = sortOrder++;
            series.BibleDocuments.Add(document);
        }

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

        var characterError = DocumentInputHelper.ValidateReferenceDocuments(
            request.Characters ?? [],
            "Character");
        if (characterError is { } characterMessage)
        {
            return characterMessage;
        }

        return DocumentInputHelper.ValidateReferenceDocuments(request.Locations ?? [], "Location");
    }

    private static string? ValidateUpdateRequest(UpdateSeriesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Series name is required.";
        }

        if (request.Name.Trim().Length > 200)
        {
            return "Series name must be 200 characters or fewer.";
        }

        var characterError = DocumentInputHelper.ValidateReferenceDocuments(
            request.Characters ?? [],
            "Character");
        if (characterError is { } characterMessage)
        {
            return characterMessage;
        }

        return DocumentInputHelper.ValidateReferenceDocuments(request.Locations ?? [], "Location");
    }

    private static IEnumerable<SeriesBibleDocument> MapCategoryDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        string category,
        DateTimeOffset createdAt)
    {
        foreach (var mapped in DocumentInputHelper.MapReferenceDocuments(documents))
        {
            yield return new SeriesBibleDocument
            {
                Category = category,
                FileName = mapped.FileName,
                MimeType = mapped.MimeType,
                TextContent = mapped.TextContent,
                BinaryContent = mapped.BinaryContent,
                CreatedAt = createdAt,
            };
        }
    }

    private static SeriesSummaryDto ToSummaryDto(LoreSeries series) =>
        new(
            series.Id,
            series.Name,
            series.BibleDocuments.Count(document => document.Category == BibleDocumentCategories.Character),
            series.BibleDocuments.Count(document => document.Category == BibleDocumentCategories.Location),
            series.CreatedAt,
            series.UpdatedAt);

    private static SeriesDetailDto ToDetailDto(LoreSeries series) =>
        new(
            series.Id,
            series.Name,
            [
                ..series.BibleDocuments
                    .OrderBy(document => document.SortOrder)
                    .Select(document => new SeriesBibleDocumentDto(
                        document.Id,
                        document.Category,
                        document.FileName,
                        document.MimeType,
                        document.TextContent,
                        ToBase64(document.BinaryContent),
                        document.SortOrder)),
            ],
            series.CreatedAt,
            series.UpdatedAt);

    private static string? ToBase64(byte[]? content) =>
        content is { Length: > 0 } ? Convert.ToBase64String(content) : null;
}
