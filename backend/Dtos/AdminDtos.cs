using backend.Models;

namespace backend.Dtos;

public record CollectionSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int FieldCount,
    int EntryCount,
    DateTimeOffset UpdatedAt);

public record CollectionDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CollectionFieldDto> Fields);

public record CollectionFieldDto(
    Guid Id,
    string Name,
    string FieldKey,
    FieldType FieldType,
    bool IsRequired,
    int DisplayOrder);

public record CreateCollectionRequest(string Name, string Slug, string? Description);

public record CreateCollectionFieldRequest(
    string Name,
    string FieldKey,
    FieldType FieldType,
    bool IsRequired,
    int DisplayOrder);

public record CollectionEntryDto(
    Guid Id,
    Guid CollectionId,
    Dictionary<string, object?> Values,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateCollectionEntryRequest(Dictionary<string, object?> Values);

public record UpdateCollectionEntryRequest(Dictionary<string, object?> Values);

public record SchemaInfoDto(string Name);

public record SchemaObjectDto(string Name, string Type);

public record ColumnInfoDto(
    string Name,
    string DataType,
    bool IsNullable,
    string? DefaultValue);
