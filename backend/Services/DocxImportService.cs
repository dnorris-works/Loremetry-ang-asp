using backend.Dtos;

namespace backend.Services;

public sealed record ExpandedStoryDocuments(
    IReadOnlyList<StoryDocumentInputDto> Manuscripts,
    IReadOnlyList<StoryDocumentInputDto> Characters,
    IReadOnlyList<StoryDocumentInputDto> Locations);

public static class DocxImportService
{
    private const string MarkdownMime = "text/markdown";

    public static ExpandedStoryDocuments ExpandStoryDocuments(
        IReadOnlyList<StoryDocumentInputDto> manuscripts,
        IReadOnlyList<StoryDocumentInputDto> characters,
        IReadOnlyList<StoryDocumentInputDto> locations)
    {
        var expandedManuscripts = new List<StoryDocumentInputDto>();
        var expandedCharacters = new List<StoryDocumentInputDto>();
        var expandedLocations = new List<StoryDocumentInputDto>();

        foreach (var document in manuscripts)
        {
            if (IsDocx(document))
            {
                var parsed = ParseDocx(document, DocxImportTarget.Manuscript);
                expandedManuscripts.AddRange(parsed.ManuscriptChapters);
                expandedCharacters.AddRange(parsed.Characters);
                expandedLocations.AddRange(parsed.Locations);
                continue;
            }

            expandedManuscripts.Add(document);
        }

        foreach (var document in characters)
        {
            if (IsDocx(document))
            {
                var parsed = ParseDocx(document, DocxImportTarget.Character);
                expandedCharacters.AddRange(parsed.Characters);
                expandedManuscripts.AddRange(parsed.ManuscriptChapters);
                expandedLocations.AddRange(parsed.Locations);
                continue;
            }

            expandedCharacters.Add(document);
        }

        foreach (var document in locations)
        {
            if (IsDocx(document))
            {
                var parsed = ParseDocx(document, DocxImportTarget.Location);
                expandedLocations.AddRange(parsed.Locations);
                expandedManuscripts.AddRange(parsed.ManuscriptChapters);
                expandedCharacters.AddRange(parsed.Characters);
                continue;
            }

            expandedLocations.Add(document);
        }

        return new ExpandedStoryDocuments(expandedManuscripts, expandedCharacters, expandedLocations);
    }

    public static IReadOnlyList<StoryDocumentInputDto> ExpandCategoryDocuments(
        IReadOnlyList<StoryDocumentInputDto> documents,
        DocxImportTarget target)
    {
        var expanded = new List<StoryDocumentInputDto>();

        foreach (var document in documents)
        {
            if (!IsDocx(document))
            {
                expanded.Add(document);
                continue;
            }

            var parsed = ParseDocx(document, target);
            expanded.AddRange(target switch
            {
                DocxImportTarget.Character => parsed.Characters,
                DocxImportTarget.Location => parsed.Locations,
                _ => parsed.ManuscriptChapters,
            });
        }

        return expanded;
    }

    private static IReadOnlyList<StoryDocumentInputDto> ToDtos(IReadOnlyList<DocxTextSection> sections) =>
    [
        ..sections.Select(section => new StoryDocumentInputDto(
            section.FileName,
            MarkdownMime,
            section.Content,
            null)),
    ];

    private static ParsedDocxDocuments ParseDocx(StoryDocumentInputDto document, DocxImportTarget target)
    {
        var bytes = DecodeBase64(document.BinaryContentBase64)
            ?? throw new InvalidOperationException($"Could not read DOCX file '{document.FileName}'.");

        var markdown = DocxTextExtractor.ExtractMarkdown(bytes);
        var parsed = DocxContentParser.Parse(markdown, document.FileName, target);

        return new ParsedDocxDocuments(
            ToDtos(parsed.ManuscriptChapters),
            ToDtos(parsed.Characters),
            ToDtos(parsed.Locations));
    }

    private static bool IsDocx(StoryDocumentInputDto document) =>
        Path.GetExtension(document.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase);

    private static byte[]? DecodeBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(value.Trim());
            return bytes.Length > 0 ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private sealed record ParsedDocxDocuments(
        IReadOnlyList<StoryDocumentInputDto> ManuscriptChapters,
        IReadOnlyList<StoryDocumentInputDto> Characters,
        IReadOnlyList<StoryDocumentInputDto> Locations);
}
