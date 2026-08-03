namespace backend.Models;

public class CollectionEntry
{
    public Guid Id { get; set; }

    public Guid CollectionId { get; set; }

    public Collection Collection { get; set; } = null!;

    public string ValuesJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
