using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Endpoints;

public static class SettingsEndpoints
{
    private static readonly HashSet<string> ThemeValues =
    [
        "light",
        "dark",
        "system",
        "slate",
        "ocean",
        "forest",
        "rose",
        "sunset",
        "high-contrast",
    ];

    private static readonly IReadOnlyDictionary<string, string> DefaultValues =
        new Dictionary<string, string>
        {
            [AppSettingKeys.Theme] = "system",
        };

    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settings = app.MapGroup("/api/settings");

        settings.MapGet("/", GetSettings);
        settings.MapPut("/{key}", UpdateSetting);

        return app;
    }

    private static async Task<IResult> GetSettings(
        HttpRequest request,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var authResult = await TryResolveUser(request, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        var persisted = await db.UserSettings
            .AsNoTracking()
            .Where(setting => setting.UserId == authResult.User!.DbUserId)
            .ToDictionaryAsync(setting => setting.SettingKey, cancellationToken);

        var settings = DefaultValues.Keys
            .OrderBy(key => key)
            .Select(key =>
            {
                if (persisted.TryGetValue(key, out var setting))
                {
                    return new AppSettingDto(setting.SettingKey, setting.Value, setting.UpdatedAt);
                }

                return new AppSettingDto(key, DefaultValues[key], DateTimeOffset.UtcNow);
            })
            .ToList();

        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSetting(
        string key,
        UpdateAppSettingRequest request,
        HttpRequest httpRequest,
        AuthService authService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var authResult = await TryResolveUser(httpRequest, authService, cancellationToken);
        if (authResult.Error is { } error)
        {
            return error;
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return Results.BadRequest(new { message = "Value is required." });
        }

        var validationError = ValidateSettingValue(key, request.Value);
        if (validationError is { } errorMessage)
        {
            return Results.BadRequest(new { message = errorMessage });
        }

        if (!DefaultValues.ContainsKey(key))
        {
            return Results.NotFound(new { message = $"Setting '{key}' was not found." });
        }

        var userId = authResult.User!.DbUserId;
        var setting = await db.UserSettings.FirstOrDefaultAsync(
            item => item.UserId == userId && item.SettingKey == key,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (setting is null)
        {
            setting = new UserSetting
            {
                UserId = userId,
                SettingKey = key,
                Value = request.Value.Trim(),
                UpdatedAt = now,
            };
            db.UserSettings.Add(setting);
        }
        else
        {
            setting.Value = request.Value.Trim();
            setting.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new AppSettingDto(setting.SettingKey, setting.Value, setting.UpdatedAt));
    }

    private static string? ValidateSettingValue(string key, string value) =>
        key switch
        {
            AppSettingKeys.Theme when !ThemeValues.Contains(value) => $"Theme '{value}' is not supported.",
            _ => null,
        };

    private static async Task<(AuthUser? User, IResult? Error)> TryResolveUser(
        HttpRequest request,
        AuthService authService,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await authService.ResolveAsync(request.Headers, cancellationToken);
            return (user, null);
        }
        catch (AuthException exception)
        {
            return (null, Results.Json(new { message = exception.Message }, statusCode: StatusCodes.Status401Unauthorized));
        }
    }
}

public static class AppSettingKeys
{
    public const string Theme = "theme";
}
