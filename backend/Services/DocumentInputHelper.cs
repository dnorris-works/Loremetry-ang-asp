using backend.Dtos;

namespace backend.Services;

public static class DocumentInputHelper
{
    private static readonly HashSet<string> AllowedExtensions = [".md", ".txt", ".docx"];

    public static string? ValidateBibleDocuments(IReadOnlyList<StoryDocumentInputDto> documents) =>
        ValidateDocuments(documents, "bible");

    public static string? ValidateManuscriptDocuments(IReadOnlyList<StoryDocumentInputDto> documents) =>
        ValidateDocuments(documents, "manuscript", requireMarkdownContent: true);

    public static IReadOnlyList<MappedDocumentContent> MapBibleDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents) =>
        documents.Select(MapBibleDocument).ToList();

    public static MappedDocumentContent MapBibleDocument(StoryDocumentInputDto document)
    {
        var fileName = document.FileName.Trim();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        string? textContent = null;
        byte[]? binaryContent = null;

        if (extension is ".md" or ".txt")
        {
            textContent = document.TextContent!.Trim();
        }
        else if (extension == ".docx")
        {
            TryDecodeBase64(document.BinaryContentBase64, out binaryContent);
        }

        return new MappedDocumentContent(
            fileName,
            document.MimeType.Trim(),
            textContent,
            binaryContent);
    }

    public static MappedDocumentContent MapManuscriptDocument(StoryDocumentInputDto document)
    {
        var fileName = document.FileName.Trim();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        string? textContent = extension == ".md" ? document.TextContent!.Trim() : null;

        return new MappedDocumentContent(
            fileName,
            document.MimeType.Trim(),
            textContent,
            null);
    }

    private static string? ValidateDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        string kind,
        bool requireMarkdownContent = false)
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

            if (kind == "manuscript" && extension == ".md" && !hasText)
            {
                return $"Markdown manuscript '{fileName}' must include text content.";
            }

            if (kind == "bible")
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

public sealed record MappedDocumentContent(
    string FileName,
    string MimeType,
    string? TextContent,
    byte[]? BinaryContent);
