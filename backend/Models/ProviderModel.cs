namespace backend.Models;

public class ProviderModel
{
    public string Id { get; set; } = string.Empty;

    public string Provider { get; set; } = "tokenmix";

    public string OwnedBy { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ModelType { get; set; } = string.Empty;

    public double? InputPrice { get; set; }

    public double? OutputPrice { get; set; }

    public string InputPriceUnit { get; set; } = "per_million";

    public string OutputPriceUnit { get; set; } = "per_million";

    public int SortOrder { get; set; }

    public DateTimeOffset SyncedAt { get; set; }
}
