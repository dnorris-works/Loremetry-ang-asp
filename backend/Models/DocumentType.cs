namespace backend.Models;

public class DocumentType
{
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool AppliesToStory { get; set; }

    public bool AppliesToSeries { get; set; }

    public bool Active { get; set; }
}
