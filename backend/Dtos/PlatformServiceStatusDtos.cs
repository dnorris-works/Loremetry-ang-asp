namespace backend.Dtos;

public record PlatformServiceStatusDto(
    string Service,
    string Label,
    string State,
    string? Message,
    DateTimeOffset? TestedAt,
    bool Configured);
