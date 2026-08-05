namespace backend.Dtos;

public record PlatformServiceTestResult(
    string Service,
    string Label,
    bool Configured,
    bool Success,
    string? Error);

public record PlatformConnectionTestResult(IReadOnlyList<PlatformServiceTestResult> Results);

public record TestPlatformSettingsRequest(
    string AnthropicApiKey,
    string TokenmixApiKey,
    string CanopyApiKey,
    string DataForSeoLogin,
    string DataForSeoPassword,
    string DefaultProvider,
    string DefaultModel,
    string CanopyPricingPlan);
