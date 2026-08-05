namespace backend.Dtos;

public record UpsertWritingDraftRequest(
    string DraftKey,
    string Mode,
    string? Source,
    long? ParentId,
    long? DocumentId,
    string Category,
    string Title,
    string FileName,
    string TextContent,
    string DestinationKey,
    string MimeType);

public record WritingDraftDto(
    string DraftKey,
    string Mode,
    string? Source,
    long? ParentId,
    long? DocumentId,
    string Category,
    string Title,
    string FileName,
    string TextContent,
    string DestinationKey,
    string MimeType,
    DateTimeOffset UpdatedAt);
