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
        var issuer = ReadEnv("CLERK_JWT_ISSUER", "clerk_jwt_issuer").TrimEnd('/');
        var bootstrapEmail = ReadEnv("BOOTSTRAP_ADMIN_EMAIL", "bootstrap_admin_email");
        var operatorKey = ReadEnv("OPERATOR_KEY", "admin_bypass_token");

        return new ClerkOptions
        {
            PublishableKey = ReadEnv("CLERK_PUBLISHABLE_KEY", "clerk_publishable_key"),
            JwtIssuer = issuer,
            AdminBypassToken = operatorKey,
            BootstrapAdminEmail = string.IsNullOrWhiteSpace(bootstrapEmail) ? "admin@local" : bootstrapEmail,
        };
    }

    private static string ReadEnv(string primaryKey, string? legacyKey = null)
    {
        var value = Environment.GetEnvironmentVariable(primaryKey);
        if (string.IsNullOrWhiteSpace(value) && legacyKey is not null)
        {
            value = Environment.GetEnvironmentVariable(legacyKey);
        }

        return (value ?? string.Empty).Trim();
    }
}
