using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class StoryService
{
    public static async Task<IReadOnlyList<StorySummaryDto>> ListForUserAsync(
        long userId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var stories = await db.Stories
            .AsNoTracking()
            .Where(story => story.UserId == userId)
            .OrderByDescending(story => story.UpdatedAt)
            .Select(story => new
            {
                story.Id,
                story.Name,
                story.SeriesId,
                story.SeriesSortOrder,
                story.CreatedAt,
                story.UpdatedAt,
                ManuscriptCount = story.Documents.Count(document => document.Kind == StoryDocumentKinds.Manuscript),
                CharacterCount = story.Documents.Count(document => document.Kind == StoryDocumentKinds.Character),
                LocationCount = story.Documents.Count(document => document.Kind == StoryDocumentKinds.Location),
            })
            .ToListAsync(cancellationToken);

        return
        [
            ..stories.Select(story => new StorySummaryDto(
                story.Id,
                story.Name,
                story.SeriesId,
                story.SeriesSortOrder,
                story.ManuscriptCount,
                story.CharacterCount,
                story.LocationCount,
                story.CreatedAt,
                story.UpdatedAt)),
        ];
    }

    public static async Task<StoryDetailDto?> GetForUserAsync(
        long userId,
        long storyId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var story = await db.Stories
            .AsNoTracking()
            .Include(item => item.Documents.OrderBy(document => document.SortOrder))
            .FirstOrDefaultAsync(item => item.Id == storyId && item.UserId == userId, cancellationToken);

        return story is null ? null : ToDetailDto(story);
    }

    public static async Task<StorySummaryDto> CreateAsync(
        long userId,
        CreateStoryRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var expanded = ExpandStoryRequest(request);
        var validationError = ValidateCreateRequest(expanded);
        if (validationError is { } message)
        {
            throw new InvalidOperationException(message);
        }

        var now = DateTimeOffset.UtcNow;
        var story = new Story
        {
            UserId = userId,
            Name = request.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        List<LoreStoryDocument> documents =
        [
            ..MapManuscriptDocuments(expanded.Manuscripts, now),
            ..MapReferenceDocuments(expanded.Characters, StoryDocumentKinds.Character, now),
            ..MapReferenceDocuments(expanded.Locations, StoryDocumentKinds.Location, now),
        ];

        foreach (var (document, index) in documents.Select((document, index) => (document, index)))
        {
            document.SortOrder = index;
            story.Documents.Add(document);
        }

        db.Stories.Add(story);
        await db.SaveChangesAsync(cancellationToken);

        return ToSummaryDto(story);
    }

    public static async Task<StorySummaryDto?> UpdateAsync(
        long userId,
        long storyId,
        UpdateStoryRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var expanded = ExpandStoryRequest(request);
        var validationError = ValidateUpdateRequest(expanded);
        if (validationError is { } message)
        {
            throw new InvalidOperationException(message);
        }

        var story = await db.Stories
            .Include(item => item.Documents)
            .FirstOrDefaultAsync(item => item.Id == storyId && item.UserId == userId, cancellationToken);

        if (story is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        story.Name = request.Name.Trim();
        story.UpdatedAt = now;
        db.StoryDocuments.RemoveRange(story.Documents);
        story.Documents.Clear();

        List<LoreStoryDocument> documents =
        [
            ..MapManuscriptDocuments(expanded.Manuscripts, now),
            ..MapReferenceDocuments(expanded.Characters, StoryDocumentKinds.Character, now),
            ..MapReferenceDocuments(expanded.Locations, StoryDocumentKinds.Location, now),
        ];

        foreach (var (document, index) in documents.Select((document, index) => (document, index)))
        {
            document.StoryId = story.Id;
            document.SortOrder = index;
            story.Documents.Add(document);
        }

        await db.SaveChangesAsync(cancellationToken);

        return ToSummaryDto(story);
    }

    public static async Task<StoryDocumentDto?> UpdateDocumentTextAsync(
        long userId,
        long storyId,
        long documentId,
        string textContent,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var document = await db.StoryDocuments
            .Include(item => item.Story)
            .FirstOrDefaultAsync(
                item => item.Id == documentId && item.StoryId == storyId && item.Story.UserId == userId,
                cancellationToken);

        if (document is null)
        {
            return null;
        }

        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
        if (extension is not ".md" and not ".txt")
        {
            throw new InvalidOperationException($"File '{document.FileName}' cannot be edited as text.");
        }

        document.TextContent = textContent;
        document.BinaryContent = null;
        document.Story.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new StoryDocumentDto(
            document.Id,
            document.Kind,
            document.FileName,
            document.MimeType,
            document.TextContent,
            ToBase64(document.BinaryContent),
            document.SortOrder);
    }

    public static async Task<StorySummaryDto?> AssignToSeriesAsync(
        long userId,
        long storyId,
        AssignStorySeriesRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var story = await db.Stories
            .Include(item => item.Documents)
            .FirstOrDefaultAsync(item => item.Id == storyId && item.UserId == userId, cancellationToken);

        if (story is null)
        {
            return null;
        }

        if (request.SeriesId is { } seriesId)
        {
            var seriesExists = await db.Series
                .AnyAsync(item => item.Id == seriesId && item.UserId == userId, cancellationToken);

            if (!seriesExists)
            {
                throw new InvalidOperationException($"Series '{seriesId}' was not found.");
            }

            var maxOrder = await db.Stories
                .Where(item => item.SeriesId == seriesId && item.UserId == userId)
                .MaxAsync(item => (int?)item.SeriesSortOrder, cancellationToken) ?? -1;

            story.SeriesId = seriesId;
            story.SeriesSortOrder = maxOrder + 1;
        }
        else
        {
            story.SeriesId = null;
            story.SeriesSortOrder = 0;
        }

        story.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToSummaryDto(story);
    }

    public static async Task<IReadOnlyList<StorySummaryDto>?> ReorderStoriesInSeriesAsync(
        long userId,
        long seriesId,
        ReorderSeriesStoriesRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var seriesExists = await db.Series
            .AnyAsync(item => item.Id == seriesId && item.UserId == userId, cancellationToken);

        if (!seriesExists)
        {
            return null;
        }

        var requestedIds = request.StoryIds.ToList();
        if (requestedIds.Count == 0)
        {
            throw new InvalidOperationException("At least one story is required.");
        }

        if (requestedIds.Distinct().Count() != requestedIds.Count)
        {
            throw new InvalidOperationException("Duplicate story IDs are not allowed.");
        }

        var stories = await db.Stories
            .Include(item => item.Documents)
            .Where(item => item.UserId == userId && requestedIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (stories.Count != requestedIds.Count)
        {
            throw new InvalidOperationException("One or more stories were not found.");
        }

        var currentSeriesStoryIds = await db.Stories
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.SeriesId == seriesId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        var missingFromRequest = currentSeriesStoryIds.Except(requestedIds).ToList();
        if (missingFromRequest.Count > 0)
        {
            throw new InvalidOperationException("Story order must include every story in the series.");
        }

        foreach (var story in stories)
        {
            if (story.SeriesId is { } existingSeriesId && existingSeriesId != seriesId)
            {
                throw new InvalidOperationException($"Story '{story.Id}' belongs to a different series.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var storiesById = stories.ToDictionary(story => story.Id);

        for (var index = 0; index < requestedIds.Count; index++)
        {
            var story = storiesById[requestedIds[index]];
            story.SeriesId = seriesId;
            story.SeriesSortOrder = index;
            story.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return
        [
            ..requestedIds.Select(id => ToSummaryDto(storiesById[id])),
        ];
    }

    private static CreateStoryRequest ExpandStoryRequest(CreateStoryRequest request)
    {
        var expanded = ExpandStoryDocuments(
            request.Manuscripts,
            request.Characters,
            request.Locations);

        return request with
        {
            Manuscripts = expanded.Manuscripts,
            Characters = expanded.Characters,
            Locations = expanded.Locations,
        };
    }

    private static UpdateStoryRequest ExpandStoryRequest(UpdateStoryRequest request)
    {
        var expanded = ExpandStoryDocuments(
            request.Manuscripts,
            request.Characters,
            request.Locations);

        return request with
        {
            Manuscripts = expanded.Manuscripts,
            Characters = expanded.Characters,
            Locations = expanded.Locations,
        };
    }

    private static ExpandedStoryDocuments ExpandStoryDocuments(
        IReadOnlyList<StoryDocumentInputDto> manuscripts,
        IReadOnlyList<StoryDocumentInputDto> characters,
        IReadOnlyList<StoryDocumentInputDto> locations)
    {
        try
        {
            return DocxImportService.ExpandStoryDocuments(
                manuscripts,
                characters ?? [],
                locations ?? []);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Failed to extract content from one or more DOCX files.",
                exception);
        }
    }

    private static string? ValidateCreateRequest(CreateStoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Story name is required.";
        }

        if (request.Name.Trim().Length > 200)
        {
            return "Story name must be 200 characters or fewer.";
        }

        if (request.Manuscripts is null || request.Manuscripts.Count == 0)
        {
            return "At least one manuscript file is required.";
        }

        var manuscriptError = DocumentInputHelper.ValidateManuscriptDocuments(request.Manuscripts);
        if (manuscriptError is { } manuscriptMessage)
        {
            return manuscriptMessage;
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

    private static string? ValidateUpdateRequest(UpdateStoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Story name is required.";
        }

        if (request.Name.Trim().Length > 200)
        {
            return "Story name must be 200 characters or fewer.";
        }

        if (request.Manuscripts is null || request.Manuscripts.Count == 0)
        {
            return "At least one manuscript file is required.";
        }

        var manuscriptError = DocumentInputHelper.ValidateManuscriptDocuments(request.Manuscripts);
        if (manuscriptError is { } manuscriptMessage)
        {
            return manuscriptMessage;
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

    private static IEnumerable<LoreStoryDocument> MapManuscriptDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        DateTimeOffset createdAt)
    {
        foreach (var document in documents)
        {
            var mapped = DocumentInputHelper.MapManuscriptDocument(document);
            yield return new LoreStoryDocument
            {
                Kind = StoryDocumentKinds.Manuscript,
                FileName = mapped.FileName,
                MimeType = mapped.MimeType,
                TextContent = mapped.TextContent,
                BinaryContent = mapped.BinaryContent,
                CreatedAt = createdAt,
            };
        }
    }

    private static IEnumerable<LoreStoryDocument> MapReferenceDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        string kind,
        DateTimeOffset createdAt)
    {
        foreach (var mapped in DocumentInputHelper.MapReferenceDocuments(documents))
        {
            yield return new LoreStoryDocument
            {
                Kind = kind,
                FileName = mapped.FileName,
                MimeType = mapped.MimeType,
                TextContent = mapped.TextContent,
                BinaryContent = mapped.BinaryContent,
                CreatedAt = createdAt,
            };
        }
    }

    private static StorySummaryDto ToSummaryDto(Story story) =>
        new(
            story.Id,
            story.Name,
            story.SeriesId,
            story.SeriesSortOrder,
            story.Documents.Count(document => document.Kind == StoryDocumentKinds.Manuscript),
            story.Documents.Count(document => document.Kind == StoryDocumentKinds.Character),
            story.Documents.Count(document => document.Kind == StoryDocumentKinds.Location),
            story.CreatedAt,
            story.UpdatedAt);

    private static StoryDetailDto ToDetailDto(Story story) =>
        new(
            story.Id,
            story.Name,
            [
                ..story.Documents
                    .OrderBy(document => document.SortOrder)
                    .Select(document => new StoryDocumentDto(
                        document.Id,
                        document.Kind,
                        document.FileName,
                        document.MimeType,
                        document.TextContent,
                        ToBase64(document.BinaryContent),
                        document.SortOrder)),
            ],
            story.CreatedAt,
            story.UpdatedAt);

    private static string? ToBase64(byte[]? content) =>
        content is { Length: > 0 } ? Convert.ToBase64String(content) : null;
}
