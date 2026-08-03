namespace backend.Dtos;

public record StorySummaryDto(
    long Id,
    string Name,
    long? SeriesId,
    int ManuscriptCount,
    int CharacterCount,
    int LocationCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record StoryDocumentDto(
    long Id,
    string Kind,
    string FileName,
    string MimeType,
    string? TextContent,
    string? BinaryContentBase64,
    int SortOrder);

public record StoryDetailDto(
    long Id,
    string Name,
    IReadOnlyList<StoryDocumentDto> Documents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record StoryDocumentInputDto(
    string FileName,
    string MimeType,
    string? TextContent,
    string? BinaryContentBase64);

public record CreateStoryRequest(
    string Name,
    IReadOnlyList<StoryDocumentInputDto> Manuscripts,
    IReadOnlyList<StoryDocumentInputDto> Characters,
    IReadOnlyList<StoryDocumentInputDto> Locations);

public record UpdateStoryRequest(
    string Name,
    IReadOnlyList<StoryDocumentInputDto> Manuscripts,
    IReadOnlyList<StoryDocumentInputDto> Characters,
    IReadOnlyList<StoryDocumentInputDto> Locations);

public record AssignStorySeriesRequest(long? SeriesId);
