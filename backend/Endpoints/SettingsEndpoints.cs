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

    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settings = app.MapGroup("/api/settings");

        settings.MapGet("/", GetSettings);
        settings.MapPut("/{key}", UpdateSetting);

        return app;
    }

    private static async Task<IResult> GetSettings(AppDbContext db, CancellationToken cancellationToken)
    {
        var settings = await db.AppSettings
            .AsNoTracking()
            .OrderBy(setting => setting.SettingKey)
            .Select(setting => new AppSettingDto(setting.SettingKey, setting.Value, setting.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSetting(
        string key,
        UpdateAppSettingRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return Results.BadRequest(new { message = "Value is required." });
        }

        var validationError = ValidateSettingValue(key, request.Value);
        if (validationError is { } errorMessage)
        {
            return Results.BadRequest(new { message = errorMessage });
        }

        var setting = await db.AppSettings.FirstOrDefaultAsync(item => item.SettingKey == key, cancellationToken);
        if (setting is not { } existingSetting)
        {
            return Results.NotFound(new { message = $"Setting '{key}' was not found." });
        }

        existingSetting.Value = request.Value.Trim();
        existingSetting.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new AppSettingDto(existingSetting.SettingKey, existingSetting.Value, existingSetting.UpdatedAt));
    }

    private static string? ValidateSettingValue(string key, string value) =>
        key switch
        {
            AppSettingKeys.Theme when !ThemeValues.Contains(value) => $"Theme '{value}' is not supported.",
            _ => null,
        };
}

public static class AppSettingKeys
{
    public const string Theme = "theme";
}
