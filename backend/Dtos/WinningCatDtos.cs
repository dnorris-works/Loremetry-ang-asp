namespace backend.Dtos;

public record WinningCatImportResultDto(
    bool Success,
    int Imported,
    int SkippedOtherDepartment,
    int SkippedUnparseable,
    int StaleCount,
    string ImportedAt,
    string? Error);

public record WinningCatCatalogStatusDto(
    bool Success,
    bool HasData,
    bool Ready,
    long KindleCount,
    long BooksCount,
    long TotalCount,
    string? LastImportAt,
    string Message,
    string? Error);

public record WinningCatRemoveStaleRequest(string Since);

public record WinningCatStaleCleanupResultDto(
    bool Success,
    int Removed,
    string? Error);
