namespace backend.Configuration;

public static class PlatformSettingKeys
{
    public const string AnthropicApiKey = "anthropic_api_key";
    public const string TokenmixApiKey = "tokenmix_api_key";
    public const string CanopyApiKey = "canopy_api_key";
    public const string DataForSeoLogin = "dataforseo_login";
    public const string DataForSeoPassword = "dataforseo_password";
    public const string DefaultProvider = "default_provider";

    public static readonly string[] All =
    [
        AnthropicApiKey,
        TokenmixApiKey,
        CanopyApiKey,
        DataForSeoLogin,
        DataForSeoPassword,
        DefaultProvider,
    ];

    public static string DefaultValue(string key) =>
        key switch
        {
            DefaultProvider => "tokenmix",
            _ => string.Empty,
        };

    public static string ReadFromEnvironment(string key) =>
        (Environment.GetEnvironmentVariable(key) ?? string.Empty).Trim();
}
