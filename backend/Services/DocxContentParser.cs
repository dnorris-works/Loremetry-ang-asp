using System.Text;
using System.Text.RegularExpressions;

namespace backend.Services;

public sealed record DocxTextSection(string Title, string Content, string FileName);

public sealed class DocxParsedContent
{
    public required IReadOnlyList<DocxTextSection> ManuscriptChapters { get; init; }

    public required IReadOnlyList<DocxTextSection> Characters { get; init; }

    public required IReadOnlyList<DocxTextSection> Locations { get; init; }
}

public static class DocxContentParser
{
    private const string MarkdownMime = "text/markdown";

    public static DocxParsedContent Parse(string markdown, string sourceFileName, DocxImportTarget target)
    {
        var normalized = NormalizeLineEndings(markdown);
        var sourceStem = Path.GetFileNameWithoutExtension(sourceFileName);

        var sections = SplitIntoBibleSections(normalized);
        var manuscripts = sections.Manuscript.Length > 0
            ? SplitManuscriptChapters(sections.Manuscript, sourceStem)
            : [];
        var characters = sections.Characters.Length > 0
            ? SplitReferenceEntries(sections.Characters, "character")
            : [];
        var locations = sections.Locations.Length > 0
            ? SplitReferenceEntries(sections.Locations, "location")
            : [];

        if (manuscripts.Count == 0 && characters.Count == 0 && locations.Count == 0)
        {
            manuscripts = target switch
            {
                DocxImportTarget.Character => [],
                DocxImportTarget.Location => [],
                _ => SplitManuscriptChapters(normalized, sourceStem),
            };

            if (manuscripts.Count == 0 && target == DocxImportTarget.Character)
            {
                characters = SplitReferenceEntries(normalized, "character");
            }
            else if (manuscripts.Count == 0 && target == DocxImportTarget.Location)
            {
                locations = SplitReferenceEntries(normalized, "location");
            }
            else if (manuscripts.Count == 0)
            {
                manuscripts =
                [
                    new DocxTextSection(
                        sourceStem,
                        normalized,
                        $"{Slugify(sourceStem)}.md"),
                ];
            }
        }

        return new DocxParsedContent
        {
            ManuscriptChapters = manuscripts,
            Characters = characters,
            Locations = locations,
        };
    }

    private static (string Manuscript, string Characters, string Locations) SplitIntoBibleSections(string markdown)
    {
        var lines = markdown.Split('\n');
        var manuscript = new StringBuilder();
        var characters = new StringBuilder();
        var locations = new StringBuilder();
        var current = SectionKind.Manuscript;

        foreach (var line in lines)
        {
            if (IsCharactersSectionHeader(line))
            {
                current = SectionKind.Characters;
                continue;
            }

            if (IsLocationsSectionHeader(line))
            {
                current = SectionKind.Locations;
                continue;
            }

            if (IsManuscriptSectionHeader(line))
            {
                current = SectionKind.Manuscript;
                continue;
            }

            switch (current)
            {
                case SectionKind.Characters:
                    characters.AppendLine(line);
                    break;
                case SectionKind.Locations:
                    locations.AppendLine(line);
                    break;
                default:
                    manuscript.AppendLine(line);
                    break;
            }
        }

        return (manuscript.ToString().Trim(), characters.ToString().Trim(), locations.ToString().Trim());
    }

    private static List<DocxTextSection> SplitManuscriptChapters(string markdown, string sourceStem)
    {
        var lines = markdown.Split('\n');
        var chapters = new List<(string Title, StringBuilder Content)>();
        StringBuilder? currentContent = null;
        string? currentTitle = null;

        foreach (var line in lines)
        {
            if (IsChapterHeader(line, out var chapterTitle))
            {
                if (currentContent is not null && currentTitle is not null)
                {
                    chapters.Add((currentTitle, currentContent));
                }

                currentTitle = chapterTitle;
                currentContent = new StringBuilder();
                currentContent.AppendLine(line);
                continue;
            }

            if (currentContent is null)
            {
                currentTitle = sourceStem;
                currentContent = new StringBuilder();
            }

            currentContent.AppendLine(DocxMarkerPatterns.IsSceneMarkerLine(line) ? "---" : line);
        }

        if (currentContent is not null && currentTitle is not null && currentContent.Length > 0)
        {
            chapters.Add((currentTitle, currentContent));
        }

        if (chapters.Count == 0)
        {
            return [];
        }

        return
        [
            ..chapters.Select((chapter, index) => new DocxTextSection(
                chapter.Title,
                chapter.Content.ToString().Trim(),
                BuildChapterFileName(sourceStem, chapter.Title, index + 1))),
        ];
    }

    private static List<DocxTextSection> SplitReferenceEntries(string markdown, string kind)
    {
        var lines = markdown.Split('\n');
        var entries = new List<(string Title, StringBuilder Content)>();
        StringBuilder? currentContent = null;
        string? currentTitle = null;

        foreach (var line in lines)
        {
            if (TryGetReferenceEntryTitle(line, out var entryTitle))
            {
                if (currentContent is not null && currentTitle is not null)
                {
                    entries.Add((currentTitle, currentContent));
                }

                currentTitle = entryTitle;
                currentContent = new StringBuilder();
                currentContent.AppendLine(line);
                continue;
            }

            if (currentContent is null)
            {
                continue;
            }

            currentContent.AppendLine(line);
        }

        if (currentContent is not null && currentTitle is not null && currentContent.Length > 0)
        {
            entries.Add((currentTitle, currentContent));
        }

        if (entries.Count == 0 && !string.IsNullOrWhiteSpace(markdown))
        {
            return
            [
                new DocxTextSection(
                    kind,
                    markdown.Trim(),
                    $"{kind}.md"),
            ];
        }

        return
        [
            ..entries.Select(entry => new DocxTextSection(
                entry.Title,
                entry.Content.ToString().Trim(),
                $"{Slugify(entry.Title)}.md")),
        ];
    }

    private static bool IsCharactersSectionHeader(string line) =>
        CharactersSectionExpression.IsMatch(line.Trim());

    private static bool IsLocationsSectionHeader(string line) =>
        LocationsSectionExpression.IsMatch(line.Trim());

    private static bool IsManuscriptSectionHeader(string line) =>
        ManuscriptSectionExpression.IsMatch(line.Trim());

    private static bool IsChapterHeader(string line, out string title)
    {
        var trimmed = line.Trim();
        var match = ChapterHeaderExpression.Match(trimmed);
        if (!match.Success)
        {
            title = string.Empty;
            return false;
        }

        title = trimmed.TrimStart('#').Trim();
        return true;
    }

    private static bool TryGetReferenceEntryTitle(string line, out string title)
    {
        var trimmed = line.Trim();
        var headingMatch = ReferenceHeadingExpression.Match(trimmed);
        if (headingMatch.Success)
        {
            title = headingMatch.Groups["title"].Value.Trim();
            return !IsCharactersSectionHeader(trimmed)
                && !IsLocationsSectionHeader(trimmed)
                && !IsManuscriptSectionHeader(trimmed)
                && !IsChapterHeader(trimmed, out _);
        }

        var labelMatch = ReferenceLabelExpression.Match(trimmed);
        if (labelMatch.Success)
        {
            title = labelMatch.Groups["title"].Value.Trim();
            return true;
        }

        title = string.Empty;
        return false;
    }

    private static string BuildChapterFileName(string sourceStem, string title, int index)
    {
        var slug = Slugify(title);
        if (string.IsNullOrWhiteSpace(slug) || slug.Equals(Slugify(sourceStem), StringComparison.OrdinalIgnoreCase))
        {
            slug = $"chapter-{index:00}";
        }
        else if (!slug.StartsWith("chapter", StringComparison.OrdinalIgnoreCase))
        {
            slug = $"chapter-{index:00}-{slug}";
        }

        return $"{slug}.md";
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return slug.Length > 80 ? slug[..80].Trim('-') : slug;
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private enum SectionKind
    {
        Manuscript,
        Characters,
        Locations,
    }

    private static readonly Regex CharactersSectionExpression = new(
        @"^(?:#+\s*)?characters?(?:\s+bible)?\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LocationsSectionExpression = new(
        @"^(?:#+\s*)?locations?(?:\s+bible)?\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ManuscriptSectionExpression = new(
        @"^(?:#+\s*)?(?:manuscript|chapters?)\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ChapterHeaderExpression = new(
        @"^(?:#+\s*)?(?:chapter|part)\s+(?:\d+|[ivxlc]+|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|\w+)\b.*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ReferenceHeadingExpression = new(
        @"^#{2,6}\s+(?<title>.+?)\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ReferenceLabelExpression = new(
        @"^(?:character|location)\s*:\s*(?<title>.+?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
}

public enum DocxImportTarget
{
    Manuscript,
    Character,
    Location,
}
