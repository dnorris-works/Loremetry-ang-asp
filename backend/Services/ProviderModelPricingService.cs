using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class ProviderModelPricingService
{
    public static async Task<IReadOnlyList<ProviderModelDto>> ListAsync(
        AppDbContext db,
        string provider,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();

        var models = await db.ProviderModels
            .AsNoTracking()
            .Where(model => model.Provider == normalizedProvider)
            .OrderBy(model => model.SortOrder)
            .ThenBy(model => model.DisplayName)
            .ToListAsync(cancellationToken);

        return models.Select(ToDto).ToList();
    }

    public static async Task<(double? InputPrice, double? OutputPrice)> GetPricesAsync(
        AppDbContext db,
        string provider,
        string modelId,
        CancellationToken cancellationToken)
    {
        var model = await db.ProviderModels
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Provider == provider && item.Id == modelId,
                cancellationToken);

        return model is null ? (null, null) : (model.InputPrice, model.OutputPrice);
    }

    public static AiUsageEstimateDto EstimateCost(
        string model,
        int inputTokens,
        int outputTokens,
        double? inputPricePerMillion,
        double? outputPricePerMillion)
    {
        var pricingMissing = !inputPricePerMillion.HasValue && !outputPricePerMillion.HasValue;

        var inputCost = inputPricePerMillion.HasValue
            ? inputTokens / 1_000_000.0 * inputPricePerMillion.Value
            : 0;

        var outputCost = outputPricePerMillion.HasValue
            ? outputTokens / 1_000_000.0 * outputPricePerMillion.Value
            : 0;

        var total = Math.Round(inputCost + outputCost, 6);

        return new AiUsageEstimateDto(model, inputTokens, outputTokens, total, pricingMissing);
    }

    public static async Task<AiUsageEvent> RecordLlmUsageAsync(
        AppDbContext db,
        long userId,
        string provider,
        string model,
        string feature,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken)
    {
        var (inputPrice, outputPrice) = await GetPricesAsync(db, provider, model, cancellationToken);
        var estimate = EstimateCost(model, inputTokens, outputTokens, inputPrice, outputPrice);

        var metadata = estimate.PricingMissing
            ? """{"pricingMissing":true}"""
            : null;

        var usageEvent = new AiUsageEvent
        {
            UserId = userId,
            Kind = "llm",
            Provider = provider,
            Model = model,
            Feature = feature,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CostUsd = estimate.CostUsd,
            MetadataJson = metadata,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.AiUsageEvents.Add(usageEvent);
        await db.SaveChangesAsync(cancellationToken);

        return usageEvent;
    }

    private static ProviderModelDto ToDto(ProviderModel model) =>
        new(
            model.Id,
            model.Provider,
            model.OwnedBy,
            model.DisplayName,
            model.ModelType,
            model.InputPrice,
            model.OutputPrice,
            model.InputPriceUnit,
            model.OutputPriceUnit,
            model.SortOrder,
            model.SyncedAt);
}
