namespace backend.Dtos;

public record SeriesSummaryDto(
    long Id,
    string Name,
    int BibleCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SeriesBibleDocumentDto(
    long Id,
    string FileName,
    string MimeType,
    bool HasTextContent,
    bool HasBinaryContent,
    int SortOrder);

public record SeriesDetailDto(
    long Id,
    string Name,
    IReadOnlyList<SeriesBibleDocumentDto> BibleDocuments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateSeriesRequest(
    string Name,
    IReadOnlyList<StoryDocumentInputDto> Bibles);
