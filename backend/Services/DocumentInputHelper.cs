using backend.Dtos;

namespace backend.Services;

public static class DocumentInputHelper
{
    private static readonly HashSet<string> AllowedExtensions = [".md", ".txt", ".docx"];

    public static string? ValidateReferenceDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        string label) =>
        ValidateDocuments(documents, label, isReferenceDocument: true);

    public static string? ValidateManuscriptDocuments(IReadOnlyList<StoryDocumentInputDto> documents) =>
        ValidateDocuments(documents, "manuscript", requireMarkdownContent: true);

    public static IReadOnlyList<MappedDocumentContent> MapReferenceDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents) =>
        [..documents.Select(MapReferenceDocument)];

    public static MappedDocumentContent MapReferenceDocument(StoryDocumentInputDto document)
    {
        var fileName = document.FileName.Trim();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var textContent = extension is ".md" or ".txt" ? document.TextContent!.Trim() : null;

        return new MappedDocumentContent(
            fileName,
            document.MimeType.Trim(),
            textContent,
            null);
    }

    public static MappedDocumentContent MapManuscriptDocument(StoryDocumentInputDto document)
    {
        var fileName = document.FileName.Trim();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        string? textContent = extension is ".md" or ".txt" ? document.TextContent!.Trim() : null;

        return new MappedDocumentContent(
            fileName,
            document.MimeType.Trim(),
            textContent,
            null);
    }

    private static string? ValidateDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        string kind,
        bool requireMarkdownContent = false,
        bool isReferenceDocument = false)
    {
        HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);

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

            if (kind == "manuscript" && extension is ".md" or ".txt" && !hasText)
            {
                return $"Manuscript '{fileName}' must include text content.";
            }

            if (isReferenceDocument)
            {
                if (extension is ".md" or ".txt" && !hasText)
                {
                    return $"{kind} file '{fileName}' must include text content.";
                }

                if (extension == ".docx")
                {
                    return $"{kind} file '{fileName}' must be extracted before save.";
                }
            }

            if (requireMarkdownContent && extension == ".md" && !hasText)
            {
                return $"Markdown file '{fileName}' must include text content.";
            }
        }

        return null;
    }

    private static bool HasAllowedExtension(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
}

public sealed record MappedDocumentContent(
    string FileName,
    string MimeType,
    string? TextContent,
    byte[]? BinaryContent);
