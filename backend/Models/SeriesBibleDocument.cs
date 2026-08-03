namespace backend.Models;

public class SeriesBibleDocument
{
    public long Id { get; set; }

    public long SeriesId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public string? TextContent { get; set; }

    public byte[]? BinaryContent { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public LoreSeries Series { get; set; } = null!;
}
