namespace backend.Models;

public class AiUsageEvent
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Kind { get; set; } = "llm";

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Feature { get; set; } = string.Empty;

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public double CostUsd { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
