using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend.Services;

public static class DocxTextExtractor
{
    public static string ExtractMarkdown(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("DOCX file is empty.");
        }

        using var stream = new MemoryStream(bytes);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException("DOCX file has no readable body.");

        var builder = new StringBuilder();

        foreach (var element in body.Elements())
        {
            if (element is not Paragraph paragraph)
            {
                continue;
            }

            var text = paragraph.InnerText.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                builder.AppendLine();
                continue;
            }

            if (DocxMarkerPatterns.IsSceneMarkerLine(text))
            {
                builder.AppendLine("---");
                continue;
            }

            var headingPrefix = GetHeadingPrefix(paragraph);
            builder.AppendLine($"{headingPrefix}{text}");
        }

        var markdown = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new InvalidOperationException("DOCX file contains no extractable text.");
        }

        return markdown;
    }

    private static string GetHeadingPrefix(Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrWhiteSpace(styleId))
        {
            return string.Empty;
        }

        return styleId.ToLowerInvariant() switch
        {
            "heading1" or "1" or "title" => "# ",
            "heading2" or "2" => "## ",
            "heading3" or "3" => "### ",
            _ when styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(styleId[^1..], out var level) && level is >= 1 and <= 6
                => $"{new string('#', level)} ",
            _ => string.Empty,
        };
    }
}
