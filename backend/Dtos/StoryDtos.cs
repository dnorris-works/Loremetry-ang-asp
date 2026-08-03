namespace backend.Dtos;

public record StorySummaryDto(
    long Id,
    string Name,
    int ManuscriptCount,
    int BibleCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record StoryDocumentDto(
    long Id,
    string Kind,
    string FileName,
    string MimeType,
    bool HasTextContent,
    bool HasBinaryContent,
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
    IReadOnlyList<StoryDocumentInputDto> Bibles);
