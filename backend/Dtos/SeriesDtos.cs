namespace backend.Dtos;

public record SeriesSummaryDto(
    long Id,
    string Name,
    int CharacterCount,
    int LocationCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SeriesBibleDocumentDto(
    long Id,
    string Category,
    string FileName,
    string MimeType,
    string? TextContent,
    string? BinaryContentBase64,
    int SortOrder);

public record SeriesDetailDto(
    long Id,
    string Name,
    IReadOnlyList<SeriesBibleDocumentDto> BibleDocuments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateSeriesRequest(
    string Name,
    IReadOnlyList<StoryDocumentInputDto> Characters,
    IReadOnlyList<StoryDocumentInputDto> Locations);

public record UpdateSeriesRequest(
    string Name,
    IReadOnlyList<StoryDocumentInputDto> Characters,
    IReadOnlyList<StoryDocumentInputDto> Locations);

public record ReorderSeriesStoriesRequest(IReadOnlyList<long> StoryIds);
