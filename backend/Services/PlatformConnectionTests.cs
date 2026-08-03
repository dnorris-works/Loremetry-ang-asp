using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using backend.Dtos;

namespace backend.Services;

public sealed class PlatformConnectionTests(IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    public async Task<PlatformConnectionTestResult> TestAllAsync(
        TestPlatformSettingsRequest settings,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = RequestTimeout;

        var results = await Task.WhenAll(
            TestAnthropicAsync(client, settings.AnthropicApiKey, cancellationToken),
            TestTokenmixAsync(client, settings.TokenmixApiKey, cancellationToken),
            TestCanopyAsync(client, settings.CanopyApiKey, cancellationToken),
            TestDataForSeoAsync(client, settings.DataForSeoLogin, settings.DataForSeoPassword, cancellationToken));

        return new PlatformConnectionTestResult(results);
    }

    private static async Task<PlatformServiceTestResult> TestAnthropicAsync(
        HttpClient client,
        string apiKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new PlatformServiceTestResult("anthropic", "Anthropic", false, false, null);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
        request.Headers.Add("x-api-key", apiKey.Trim());
        request.Headers.Add("anthropic-version", "2023-06-01");

        return await SendAsync(client, request, "anthropic", "Anthropic", cancellationToken);
    }

    private static async Task<PlatformServiceTestResult> TestTokenmixAsync(
        HttpClient client,
        string apiKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new PlatformServiceTestResult("tokenmix", "TokenMix AI", false, false, null);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.tokenmix.ai/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

        return await SendAsync(client, request, "tokenmix", "TokenMix AI", cancellationToken);
    }

    private static async Task<PlatformServiceTestResult> TestCanopyAsync(
        HttpClient client,
        string apiKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new PlatformServiceTestResult("canopy", "Canopy", false, false, null);
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://rest.canopyapi.co/api/amazon/autocomplete?searchTerm=book&domain=US");
        request.Headers.Add("API-KEY", apiKey.Trim());

        return await SendAsync(client, request, "canopy", "Canopy", cancellationToken);
    }

    private static async Task<PlatformServiceTestResult> TestDataForSeoAsync(
        HttpClient client,
        string login,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return new PlatformServiceTestResult("dataforseo", "DataForSEO", false, false, null);
        }

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{login.Trim()}:{password.Trim()}"));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.dataforseo.com/v3/keywords_data/google_ads/search_volume/live");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(
            """[{"keywords":["test"],"location_code":2840,"language_code":"en"}]""",
            Encoding.UTF8,
            "application/json");

        return await SendAsync(client, request, "dataforseo", "DataForSEO", cancellationToken);
    }

    private static async Task<PlatformServiceTestResult> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        string service,
        string label,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new PlatformServiceTestResult(service, label, true, true, null);
            }

            var error = ExtractErrorMessage(body, response.StatusCode);
            return new PlatformServiceTestResult(service, label, true, false, error);
        }
        catch (TaskCanceledException)
        {
            return new PlatformServiceTestResult(service, label, true, false, "Connection timed out.");
        }
        catch (Exception exception)
        {
            return new PlatformServiceTestResult(service, label, true, false, exception.Message);
        }
    }

    private static string ExtractErrorMessage(string body, System.Net.HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"Request failed ({(int)statusCode}).";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var errorNode))
            {
                if (errorNode.ValueKind == JsonValueKind.Object
                    && errorNode.TryGetProperty("message", out var messageNode))
                {
                    return messageNode.GetString() ?? $"Request failed ({(int)statusCode}).";
                }

                if (errorNode.ValueKind == JsonValueKind.String)
                {
                    return errorNode.GetString() ?? $"Request failed ({(int)statusCode}).";
                }
            }

            if (root.TryGetProperty("status_message", out var statusMessage))
            {
                return statusMessage.GetString() ?? $"Request failed ({(int)statusCode}).";
            }
        }
        catch (JsonException)
        {
            /* fall through */
        }

        var snippet = body.Length > 240 ? body[..240] + "…" : body;
        return $"Request failed ({(int)statusCode}): {snippet}";
    }
}
