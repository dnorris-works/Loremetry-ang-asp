using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using backend.Configuration;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace backend.Auth;

public sealed class AuthService
{
    private readonly ClerkOptions _options;
    private readonly AppDbContext _db;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public AuthService(ClerkOptions options, AppDbContext db)
    {
        _options = options;
        _db = db;

        if (options.IsEnabled)
        {
            var metadataAddress = $"{options.JwtIssuer}/.well-known/openid-configuration";
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = metadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase) });
        }
        else
        {
            _configurationManager = null!;
        }
    }

    public async Task<AuthUser> ResolveAsync(IHeaderDictionary headers, CancellationToken cancellationToken)
    {
        if (TryGetBypassToken(headers) is { } bypassToken
            && AdminBypassValid(bypassToken))
        {
            var bootstrapUser = await EnsureBootstrapUserAsync(cancellationToken);
            return ToAuthUser(bootstrapUser, breakGlass: true);
        }

        if (!_options.IsEnabled)
        {
            throw new AuthException(
                "Clerk is not configured. Sign in is unavailable until clerk_jwt_issuer is set.");
        }

        var bearerToken = GetBearerToken(headers)
            ?? throw new AuthException("Missing Authorization: Bearer session token");

        var claims = await VerifyClerkTokenAsync(bearerToken, cancellationToken);
        var user = await UpsertClerkUserAsync(claims.Subject, claims.Email, cancellationToken);
        return ToAuthUser(user, breakGlass: false);
    }

    public async Task<AuthUser?> TryResolveAsync(IHeaderDictionary headers, CancellationToken cancellationToken)
    {
        try
        {
            return await ResolveAsync(headers, cancellationToken);
        }
        catch (AuthException)
        {
            return null;
        }
    }

    private async Task<User> EnsureBootstrapUserAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.Users
            .Where(user => user.ClerkId == null || user.ClerkId == string.Empty)
            .OrderBy(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var bootstrapUser = new User
        {
            Email = _options.BootstrapAdminEmail.Trim().ToLowerInvariant(),
            FirstName = "Operator",
            LastName = "Admin",
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Users.Add(bootstrapUser);
        await _db.SaveChangesAsync(cancellationToken);
        return bootstrapUser;
    }

    private async Task<User> UpsertClerkUserAsync(
        string clerkId,
        string? email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clerkId))
        {
            throw new AuthException("JWT is missing subject.");
        }

        var normalizedEmail = NormalizeClerkEmail(clerkId, email);
        var existing = await _db.Users
            .FirstOrDefaultAsync(user => user.ClerkId == clerkId, cancellationToken);

        if (existing is null)
        {
            existing = await _db.Users
                .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            existing = new User
            {
                ClerkId = clerkId,
                Email = normalizedEmail,
                FirstName = string.Empty,
                LastName = string.Empty,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Users.Add(existing);
        }
        else
        {
            existing.ClerkId = clerkId;
            existing.Email = normalizedEmail;
            existing.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<ClerkJwtClaims> VerifyClerkTokenAsync(string token, CancellationToken cancellationToken)
    {
        var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.JwtIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, validationParameters, out _);
        var subject = principal.FindFirst("sub")?.Value ?? string.Empty;
        var email = principal.FindFirst("email")?.Value;

        return new ClerkJwtClaims(subject, email);
    }

    private bool AdminBypassValid(string presented)
    {
        var stored = _options.AdminBypassToken;
        if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(presented))
        {
            return false;
        }

        var storedBytes = Encoding.UTF8.GetBytes(stored);
        var presentedBytes = Encoding.UTF8.GetBytes(presented);
        return storedBytes.Length == presentedBytes.Length
            && CryptographicOperations.FixedTimeEquals(storedBytes, presentedBytes);
    }

    private static string? TryGetBypassToken(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(AuthConstants.AdminBypassHeader, out var values))
        {
            return null;
        }

        var token = values.ToString().Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }

    private static string? GetBearerToken(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("Authorization", out var values))
        {
            return null;
        }

        var header = values.ToString().Trim();
        const string bearerPrefix = "Bearer ";
        if (!header.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[bearerPrefix.Length..].Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }

    private static string NormalizeClerkEmail(string clerkId, string? email)
    {
        var trimmed = email?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed.ToLowerInvariant();
        }

        return $"{clerkId}@clerk.local";
    }

    private static AuthUser ToAuthUser(User user, bool breakGlass) =>
        new()
        {
            DbUserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ClerkId = user.ClerkId ?? string.Empty,
            BreakGlass = breakGlass,
        };

    private sealed record ClerkJwtClaims(string Subject, string? Email);
}

public sealed class AuthException(string message) : Exception(message);
