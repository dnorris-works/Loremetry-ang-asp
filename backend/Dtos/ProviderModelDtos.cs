namespace backend.Dtos;

public record ProviderModelDto(
    string Id,
    string Provider,
    string OwnedBy,
    string DisplayName,
    string ModelType,
    double? InputPrice,
    double? OutputPrice,
    string InputPriceUnit,
    string OutputPriceUnit,
    int SortOrder,
    DateTimeOffset SyncedAt);

public record TokenMixPricingSyncResultDto(
    bool Success,
    int Imported,
    int ChatModels,
    DateTimeOffset SyncedAt,
    string? Error);

public record AiUsageEstimateDto(
    string Model,
    int InputTokens,
    int OutputTokens,
    double CostUsd,
    bool PricingMissing);
