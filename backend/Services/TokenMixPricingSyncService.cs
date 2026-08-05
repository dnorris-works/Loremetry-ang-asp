using System.Text.Json;
using System.Text.Json.Serialization;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class TokenMixPricingSyncService(IHttpClientFactory httpClientFactory)
{
    public const string ModelsApiUrl = "https://api.tokenmix.ai/api/models";
    public const string CatalogPageUrl = "https://tokenmix.ai/models?type=chat";
    private const int PageSize = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<TokenMixPricingSyncResultDto> SyncChatModelsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ParsedCatalogModel> parsed;

        try
        {
            parsed = await FetchAllChatModelsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            return new TokenMixPricingSyncResultDto(false, 0, 0, DateTimeOffset.UtcNow, exception.Message);
        }

        if (parsed.Count == 0)
        {
            return new TokenMixPricingSyncResultDto(
                false,
                0,
                0,
                DateTimeOffset.UtcNow,
                "No chat models returned from the TokenMix catalog API.");
        }

        var syncedAt = DateTimeOffset.UtcNow;
        var sortOrder = 0;

        foreach (var model in parsed)
        {
            sortOrder++;
            var existing = await db.ProviderModels
                .FirstOrDefaultAsync(
                    item => item.Id == model.Id && item.Provider == "tokenmix",
                    cancellationToken);

            if (existing is null)
            {
                existing = new ProviderModel
                {
                    Id = model.Id,
                    Provider = "tokenmix",
                };
                db.ProviderModels.Add(existing);
            }

            existing.OwnedBy = model.OwnedBy;
            existing.DisplayName = model.DisplayName;
            existing.ModelType = model.ModelType;
            existing.InputPrice = model.InputPrice;
            existing.OutputPrice = model.OutputPrice;
            existing.InputPriceUnit = "per_million";
            existing.OutputPriceUnit = "per_million";
            existing.SortOrder = sortOrder;
            existing.SyncedAt = syncedAt;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new TokenMixPricingSyncResultDto(
            true,
            parsed.Count,
            parsed.Count,
            syncedAt,
            null);
    }

    private async Task<IReadOnlyList<ParsedCatalogModel>> FetchAllChatModelsAsync(
        CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var models = new List<ParsedCatalogModel>();
        var page = 1;
        int totalPages;

        do
        {
            var url =
                $"{ModelsApiUrl}?type=chat&sort=popular&page={page}&per_page={PageSize}";

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<TokenMixModelsResponse>(
                stream,
                JsonOptions,
                cancellationToken);

            if (payload?.Data is null || payload.Data.Count == 0)
            {
                break;
            }

            totalPages = payload.Meta?.TotalPages ?? page;
            models.AddRange(payload.Data.Select(MapCatalogModel));
            page++;
        }
        while (page <= totalPages);

        return models;
    }

    internal static ParsedCatalogModel MapCatalogModel(TokenMixModelEntry entry)
    {
        var ownedBy = entry.Vendor?.Slug?.Trim();
        if (string.IsNullOrWhiteSpace(ownedBy) && !string.IsNullOrWhiteSpace(entry.ModelId))
        {
            var slashIndex = entry.ModelId.IndexOf('/');
            if (slashIndex > 0)
            {
                ownedBy = entry.ModelId[..slashIndex];
            }
        }

        var id = ResolveModelId(entry);

        return new ParsedCatalogModel(
            Id: id,
            OwnedBy: ownedBy ?? string.Empty,
            DisplayName: entry.Name?.Trim() ?? id,
            ModelType: entry.ModelType?.Trim() ?? "chat",
            InputPrice: entry.InputPrice,
            OutputPrice: entry.OutputPrice);
    }

    internal static string ResolveModelId(TokenMixModelEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.ShortId))
        {
            return entry.ShortId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(entry.ModelId))
        {
            var slashIndex = entry.ModelId.LastIndexOf('/');
            return slashIndex >= 0
                ? entry.ModelId[(slashIndex + 1)..].Trim()
                : entry.ModelId.Trim();
        }

        return string.Empty;
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(nameof(TokenMixPricingSyncService));
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Loremetry/1.0 (+https://tokenmix.ai/models?type=chat)");
        return client;
    }

    internal sealed record ParsedCatalogModel(
        string Id,
        string OwnedBy,
        string DisplayName,
        string ModelType,
        double? InputPrice,
        double? OutputPrice);

    internal sealed class TokenMixModelsResponse
    {
        public List<TokenMixModelEntry> Data { get; set; } = [];

        public TokenMixModelsMeta? Meta { get; set; }
    }

    internal sealed class TokenMixModelsMeta
    {
        public int Page { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        public int Total { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    internal sealed class TokenMixModelEntry
    {
        [JsonPropertyName("model_id")]
        public string? ModelId { get; set; }

        [JsonPropertyName("short_id")]
        public string? ShortId { get; set; }

        public string? Name { get; set; }

        [JsonPropertyName("model_type")]
        public string? ModelType { get; set; }

        [JsonPropertyName("input_price")]
        public double? InputPrice { get; set; }

        [JsonPropertyName("output_price")]
        public double? OutputPrice { get; set; }

        public TokenMixVendor? Vendor { get; set; }
    }

    internal sealed class TokenMixVendor
    {
        public string? Slug { get; set; }
    }
}
