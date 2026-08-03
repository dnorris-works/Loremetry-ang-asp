namespace backend.Models;

public class Story
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long? SeriesId { get; set; }

    public int SeriesSortOrder { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public LoreSeries? Series { get; set; }

    public ICollection<LoreStoryDocument> Documents { get; set; } = [];
}
