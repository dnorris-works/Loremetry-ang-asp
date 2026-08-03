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
        var validationError = ValidateCreateRequest(request);
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
            ..MapManuscriptDocuments(request.Manuscripts, now),
            ..MapReferenceDocuments(request.Characters ?? [], StoryDocumentKinds.Character, now),
            ..MapReferenceDocuments(request.Locations ?? [], StoryDocumentKinds.Location, now),
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
        var validationError = ValidateUpdateRequest(request);
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
            ..MapManuscriptDocuments(request.Manuscripts, now),
            ..MapReferenceDocuments(request.Characters ?? [], StoryDocumentKinds.Character, now),
            ..MapReferenceDocuments(request.Locations ?? [], StoryDocumentKinds.Location, now),
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
