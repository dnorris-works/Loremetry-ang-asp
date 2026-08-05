namespace backend.Configuration;

public static class PlatformSettingKeys
{
    public const string AnthropicApiKey = "anthropic_api_key";
    public const string TokenmixApiKey = "tokenmix_api_key";
    public const string CanopyApiKey = "canopy_api_key";
    public const string DataForSeoLogin = "dataforseo_login";
    public const string DataForSeoPassword = "dataforseo_password";
    public const string DefaultProvider = "default_provider";
    public const string DefaultModel = "default_model";
    public const string CanopyPricingPlan = "canopy_pricing_plan";
    public const string ServiceStatusAi = "service_status_ai";

    public const string DefaultCanopyPricingPlan = "pay_as_you_go";
    public const string ServiceStatusCanopy = "service_status_canopy";
    public const string ServiceStatusDataForSeo = "service_status_dataforseo";
    public const string ServiceStatusWinningCat = "service_status_winningcat";

    public static readonly string[] All =
    [
        AnthropicApiKey,
        TokenmixApiKey,
        CanopyApiKey,
        DataForSeoLogin,
        DataForSeoPassword,
        DefaultProvider,
        DefaultModel,
        CanopyPricingPlan,
    ];

    public static string DefaultValue(string key) =>
        key switch
        {
            DefaultProvider => "tokenmix",
            CanopyPricingPlan => DefaultCanopyPricingPlan,
            _ => string.Empty,
        };

    public static string ReadFromEnvironment(string key) =>
        (Environment.GetEnvironmentVariable(key) ?? string.Empty).Trim();
}
