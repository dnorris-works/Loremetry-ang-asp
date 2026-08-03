namespace backend.Models;

public class Collection
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CollectionField> Fields { get; set; } = [];

    public ICollection<CollectionEntry> Entries { get; set; } = [];
}
