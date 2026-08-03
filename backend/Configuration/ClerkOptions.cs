namespace backend.Configuration;

public sealed class ClerkOptions
{
    public string PublishableKey { get; init; } = string.Empty;

    public string JwtIssuer { get; init; } = string.Empty;

    public string AdminBypassToken { get; init; } = string.Empty;

    public string BootstrapAdminEmail { get; init; } = "admin@local";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(JwtIssuer);

    public static ClerkOptions FromEnvironment()
    {
        var issuer = (Environment.GetEnvironmentVariable("clerk_jwt_issuer") ?? string.Empty).Trim().TrimEnd('/');
        var bootstrapEmail = (Environment.GetEnvironmentVariable("bootstrap_admin_email") ?? string.Empty).Trim();

        return new ClerkOptions
        {
            PublishableKey = (Environment.GetEnvironmentVariable("clerk_publishable_key") ?? string.Empty).Trim(),
            JwtIssuer = issuer,
            AdminBypassToken = (Environment.GetEnvironmentVariable("admin_bypass_token") ?? string.Empty).Trim(),
            BootstrapAdminEmail = string.IsNullOrWhiteSpace(bootstrapEmail) ? "admin@local" : bootstrapEmail,
        };
    }
}
