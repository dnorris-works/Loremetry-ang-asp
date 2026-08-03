using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class StoryService
{
    private static readonly HashSet<string> AllowedExtensions = [".md", ".txt", ".docx"];

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
                BibleCount = story.Documents.Count(document => document.Kind == StoryDocumentKinds.Bible),
            })
            .ToListAsync(cancellationToken);

        return stories
            .Select(story => new StorySummaryDto(
                story.Id,
                story.Name,
                story.ManuscriptCount,
                story.BibleCount,
                story.CreatedAt,
                story.UpdatedAt))
            .ToList();
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

        var documents = new List<StoryDocument>();
        documents.AddRange(MapDocuments(request.Manuscripts, StoryDocumentKinds.Manuscript, now));
        documents.AddRange(MapDocuments(request.Bibles, StoryDocumentKinds.Bible, now));

        foreach (var (document, index) in documents.Select((document, index) => (document, index)))
        {
            document.SortOrder = index;
            story.Documents.Add(document);
        }

        db.Stories.Add(story);
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

        var manuscriptError = ValidateDocuments(request.Manuscripts, StoryDocumentKinds.Manuscript, requireMarkdownContent: true);
        if (manuscriptError is { } manuscriptMessage)
        {
            return manuscriptMessage;
        }

        var bibleError = ValidateDocuments(request.Bibles ?? [], StoryDocumentKinds.Bible, requireMarkdownContent: false);
        if (bibleError is { } bibleMessage)
        {
            return bibleMessage;
        }

        return null;
    }

    private static string? ValidateDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        string kind,
        bool requireMarkdownContent)
    {
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < documents.Count; index++)
        {
            var document = documents[index];
            var fileName = document.FileName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return $"{kind} file #{index + 1} is missing a file name.";
            }

            if (fileName.Length > 500)
            {
                return $"File name '{fileName}' is too long.";
            }

            if (!seenNames.Add(fileName))
            {
                return $"Duplicate {kind} file name '{fileName}'.";
            }

            if (!HasAllowedExtension(fileName))
            {
                return $"File '{fileName}' must use .md, .txt, or .docx.";
            }

            if (string.IsNullOrWhiteSpace(document.MimeType))
            {
                return $"File '{fileName}' is missing a MIME type.";
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var hasText = !string.IsNullOrWhiteSpace(document.TextContent);
            var hasBinary = TryDecodeBase64(document.BinaryContentBase64, out _);

            if (kind == StoryDocumentKinds.Manuscript && extension == ".md" && !hasText)
            {
                return $"Markdown manuscript '{fileName}' must include text content.";
            }

            if (kind == StoryDocumentKinds.Bible)
            {
                if (extension is ".md" or ".txt" && !hasText)
                {
                    return $"Bible file '{fileName}' must include text content.";
                }

                if (extension == ".docx" && !hasBinary)
                {
                    return $"Bible file '{fileName}' must include binary content.";
                }
            }

            if (requireMarkdownContent && extension == ".md" && !hasText)
            {
                return $"Markdown file '{fileName}' must include text content.";
            }
        }

        return null;
    }

    private static IEnumerable<StoryDocument> MapDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        string kind,
        DateTimeOffset createdAt)
    {
        foreach (var document in documents)
        {
            var fileName = document.FileName.Trim();
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            string? textContent = null;
            byte[]? binaryContent = null;

            if (kind == StoryDocumentKinds.Manuscript && extension == ".md")
            {
                textContent = document.TextContent!.Trim();
            }
            else if (kind == StoryDocumentKinds.Bible)
            {
                if (extension is ".md" or ".txt")
                {
                    textContent = document.TextContent!.Trim();
                }
                else if (extension == ".docx")
                {
                    TryDecodeBase64(document.BinaryContentBase64, out binaryContent);
                }
            }

            yield return new StoryDocument
            {
                Kind = kind,
                FileName = fileName,
                MimeType = document.MimeType.Trim(),
                TextContent = textContent,
                BinaryContent = binaryContent,
                CreatedAt = createdAt,
            };
        }
    }

    private static StorySummaryDto ToSummaryDto(Story story) =>
        new(
            story.Id,
            story.Name,
            story.Documents.Count(document => document.Kind == StoryDocumentKinds.Manuscript),
            story.Documents.Count(document => document.Kind == StoryDocumentKinds.Bible),
            story.CreatedAt,
            story.UpdatedAt);

    private static StoryDetailDto ToDetailDto(Story story) =>
        new(
            story.Id,
            story.Name,
            story.Documents
                .OrderBy(document => document.SortOrder)
                .Select(document => new StoryDocumentDto(
                    document.Id,
                    document.Kind,
                    document.FileName,
                    document.MimeType,
                    !string.IsNullOrEmpty(document.TextContent),
                    document.BinaryContent is { Length: > 0 },
                    document.SortOrder))
                .ToList(),
            story.CreatedAt,
            story.UpdatedAt);

    private static bool HasAllowedExtension(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());

    private static bool TryDecodeBase64(string? value, out byte[]? bytes)
    {
        bytes = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(value.Trim());
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
