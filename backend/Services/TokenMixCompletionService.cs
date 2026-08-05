using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using backend.Configuration;
using backend.Data;
using backend.Dtos;

namespace backend.Services;

public sealed class TokenMixCompletionService(IHttpClientFactory httpClientFactory)
{
    private const string ChatCompletionsUrl = "https://api.tokenmix.ai/v1/chat/completions";
    private const string FallbackModel = "gpt-4o-mini";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(120);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<TokenMixCompletionResult> CompleteChatAsync(
        AppDbContext db,
        string systemPrompt,
        string userPrompt,
        string? modelOverride,
        CancellationToken cancellationToken)
    {
        var settings = await PlatformSettingsService.GetAsync(db, cancellationToken);

        if (!string.Equals(settings.DefaultProvider, "tokenmix", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Writing assistant requires default_provider to be tokenmix. Anthropic is not supported yet.");
        }

        if (string.IsNullOrWhiteSpace(settings.TokenmixApiKey))
        {
            throw new InvalidOperationException(
                "TokenMix is not configured. Set tokenmix_api_key in .env or Admin → Platform.");
        }

        var model = ResolveModel(settings.DefaultModel, modelOverride);
        return await SendCompletionAsync(
            settings.TokenmixApiKey,
            model,
            systemPrompt,
            userPrompt,
            cancellationToken);
    }

    private static string ResolveModel(string configuredModel, string? modelOverride)
    {
        if (!string.IsNullOrWhiteSpace(modelOverride))
        {
            return modelOverride.Trim();
        }

        if (!string.IsNullOrWhiteSpace(configuredModel))
        {
            return configuredModel.Trim();
        }

        return FallbackModel;
    }

    private async Task<TokenMixCompletionResult> SendCompletionAsync(
        string apiKey,
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = RequestTimeout;

        var body = new
        {
            model,
            max_tokens = 4096,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ExtractErrorMessage(payload) ?? $"TokenMix request failed ({(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        if (root.TryGetProperty("error", out var errorElement))
        {
            throw new InvalidOperationException(ExtractErrorMessage(errorElement) ?? "TokenMix returned an error.");
        }

        if (!root.TryGetProperty("choices", out var choices) ||
            choices.GetArrayLength() == 0 ||
            !choices[0].TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("TokenMix returned an empty response.");
        }

        var text = content.GetString()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException("TokenMix returned an empty response.");
        }

        var (inputTokens, outputTokens) = ParseUsage(root);

        return new TokenMixCompletionResult(text, model, inputTokens, outputTokens);
    }

    private static (int InputTokens, int OutputTokens) ParseUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage))
        {
            return (0, 0);
        }

        var inputTokens = usage.TryGetProperty("prompt_tokens", out var promptTokens)
            ? promptTokens.GetInt32()
            : 0;

        var outputTokens = usage.TryGetProperty("completion_tokens", out var completionTokens)
            ? completionTokens.GetInt32()
            : 0;

        return (inputTokens, outputTokens);
    }

    private static string? ExtractErrorMessage(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return ExtractErrorMessage(document.RootElement);
        }
        catch (JsonException)
        {
            return string.IsNullOrWhiteSpace(payload) ? null : payload.Trim();
        }
    }

    private static string? ExtractErrorMessage(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        if (element.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
        {
            var value = message.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        return null;
    }
}
