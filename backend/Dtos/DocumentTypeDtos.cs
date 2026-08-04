namespace backend.Dtos;

public record DocumentTypeDto(
    string Code,
    string DisplayName,
    bool AppliesToStory,
    bool AppliesToSeries);
