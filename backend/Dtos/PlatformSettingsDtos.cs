namespace backend.Dtos;

public record PlatformSettingsDto(
    string AnthropicApiKey,
    string TokenmixApiKey,
    string CanopyApiKey,
    string DataForSeoLogin,
    string DataForSeoPassword,
    string DefaultProvider,
    bool AnthropicConfigured,
    bool TokenmixConfigured,
    bool CanopyConfigured,
    bool DataForSeoConfigured);

public record UpdatePlatformSettingsRequest(
    string AnthropicApiKey,
    string TokenmixApiKey,
    string CanopyApiKey,
    string DataForSeoLogin,
    string DataForSeoPassword,
    string DefaultProvider);
