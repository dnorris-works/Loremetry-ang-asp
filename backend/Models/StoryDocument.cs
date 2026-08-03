namespace backend.Models;

public class StoryDocument
{
    public long Id { get; set; }

    public long StoryId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public string? TextContent { get; set; }

    public byte[]? BinaryContent { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Story Story { get; set; } = null!;
}
