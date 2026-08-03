namespace backend.Models;

public class Series
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<SeriesBibleDocument> BibleDocuments { get; set; } = [];
}
