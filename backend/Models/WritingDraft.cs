namespace backend.Models;

public class WritingDraft
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string DraftKey { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;

    public string? Source { get; set; }

    public long? ParentId { get; set; }

    public long? DocumentId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string TextContent { get; set; } = string.Empty;

    public string DestinationKey { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
