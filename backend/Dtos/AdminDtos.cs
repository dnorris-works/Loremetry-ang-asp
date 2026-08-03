namespace backend.Dtos;

public record SchemaInfoDto(string Name);

public record SchemaObjectDto(string Name, string Type);

public record ColumnInfoDto(
    string Name,
    string DataType,
    bool IsNullable,
    string? DefaultValue);

public record ExecuteSqlRequest(string Sql);

public record SqlQueryResultDto(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int? RowsAffected,
    string? Message);
