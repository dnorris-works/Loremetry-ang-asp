namespace backend.Dtos;

public record PlatformSettingsDto(
    string AnthropicApiKey,
    string TokenmixApiKey,
    string CanopyApiKey,
    string DataForSeoLogin,
    string DataForSeoPassword,
    string DefaultProvider,
    string DefaultModel,
    bool AnthropicConfigured,
    bool TokenmixConfigured,
    bool CanopyConfigured,
    bool DataForSeoConfigured,
    IReadOnlyList<PlatformServiceStatusDto> ServiceStatuses);

public record UpdatePlatformSettingsRequest(
    string AnthropicApiKey,
    string TokenmixApiKey,
    string CanopyApiKey,
    string DataForSeoLogin,
    string DataForSeoPassword,
    string DefaultProvider,
    string DefaultModel);

public record WritingAssistantChatRequest(
    string UserPrompt,
    string AugmentedPrompt);

public record WritingAssistantChatResponse(string Text);
