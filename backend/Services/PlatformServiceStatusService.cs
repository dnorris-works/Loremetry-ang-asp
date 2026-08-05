using System.Text.Json;
using backend.Configuration;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class PlatformServiceStatusService
{
    private const int WinningCatReadyMinPerStore = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<IReadOnlyList<PlatformServiceStatusDto>> GetAllAsync(
        AppDbContext db,
        PlatformSettingsDto settings,
        CancellationToken cancellationToken)
    {
        await RefreshWinningCatStatusAsync(db, cancellationToken);

        var aiConfigured = IsAiConfigured(settings);
        var ai = await ReadStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusAi,
            "ai",
            "AI",
            aiConfigured,
            defaultState: aiConfigured ? "never_tested" : "not_configured",
            cancellationToken);

        var canopy = await ReadStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusCanopy,
            "canopy",
            "Canopy",
            settings.CanopyConfigured,
            defaultState: settings.CanopyConfigured ? "never_tested" : "not_configured",
            cancellationToken);

        var dataForSeo = await ReadStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusDataForSeo,
            "dataforseo",
            "DataForSEO",
            settings.DataForSeoConfigured,
            defaultState: settings.DataForSeoConfigured ? "never_tested" : "not_configured",
            cancellationToken);

        var winningCat = await ReadStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusWinningCat,
            "winningcat",
            "WinningCat",
            configured: true,
            defaultState: "not_imported",
            cancellationToken);

        return [ai, canopy, dataForSeo, winningCat];
    }

    public static async Task PersistConnectionTestResultsAsync(
        AppDbContext db,
        PlatformSettingsDto settings,
        PlatformConnectionTestResult results,
        CancellationToken cancellationToken)
    {
        var resultByService = results.Results.ToDictionary(result => result.Service, StringComparer.OrdinalIgnoreCase);

        await SaveConnectionStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusAi,
            ResolveAiTestResult(settings, resultByService),
            cancellationToken);

        await SaveConnectionStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusCanopy,
            resultByService.GetValueOrDefault("canopy"),
            cancellationToken);

        await SaveConnectionStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusDataForSeo,
            resultByService.GetValueOrDefault("dataforseo"),
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task RefreshWinningCatStatusAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var kindleCount = await ScalarLongAsync(
            db,
            """
            SELECT COUNT(*)::bigint
            FROM lore.kdp_categories
            WHERE source = 'winningcat' AND store = 'Kindle'
            """,
            cancellationToken);

        var booksCount = await ScalarLongAsync(
            db,
            """
            SELECT COUNT(*)::bigint
            FROM lore.kdp_categories
            WHERE source = 'winningcat' AND store = 'Books'
            """,
            cancellationToken);

        var lastImportAt = await ScalarStringAsync(
            db,
            """
            SELECT MAX(last_seen_at)
            FROM lore.kdp_categories
            WHERE source = 'winningcat'
            """,
            cancellationToken);

        var totalCount = kindleCount + booksCount;
        string state;
        string message;

        if (totalCount == 0)
        {
            state = "not_imported";
            message = "No WinningCat catalog in the database. Import a CSV to enable KDP category matching.";
        }
        else if (kindleCount >= WinningCatReadyMinPerStore && booksCount >= WinningCatReadyMinPerStore)
        {
            state = "ready";
            message =
                $"Catalog ready — {kindleCount:N0} Kindle and {booksCount:N0} Books categories."
                + FormatLastImportSuffix(lastImportAt);
        }
        else
        {
            state = "partial";
            message =
                $"Partial catalog — {kindleCount:N0} Kindle and {booksCount:N0} Books categories."
                + FormatLastImportSuffix(lastImportAt);
        }

        await WriteStatusAsync(
            db,
            PlatformSettingKeys.ServiceStatusWinningCat,
            state,
            message,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsAiConfigured(PlatformSettingsDto settings) =>
        string.Equals(settings.DefaultProvider, "anthropic", StringComparison.OrdinalIgnoreCase)
            ? settings.AnthropicConfigured
            : settings.TokenmixConfigured;

    private static PlatformServiceTestResult? ResolveAiTestResult(
        PlatformSettingsDto settings,
        IReadOnlyDictionary<string, PlatformServiceTestResult> results)
    {
        if (string.Equals(settings.DefaultProvider, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return results.GetValueOrDefault("anthropic");
        }

        return results.GetValueOrDefault("tokenmix");
    }

    private static async Task SaveConnectionStatusAsync(
        AppDbContext db,
        string key,
        PlatformServiceTestResult? result,
        CancellationToken cancellationToken)
    {
        if (result is null)
        {
            return;
        }

        string state;
        string? message;

        if (!result.Configured)
        {
            state = "not_configured";
            message = null;
        }
        else if (result.Success)
        {
            state = "connected";
            message = null;
        }
        else
        {
            state = "failed";
            message = result.Error;
        }

        await WriteStatusAsync(db, key, state, message, DateTimeOffset.UtcNow, cancellationToken);
    }

    private static async Task<PlatformServiceStatusDto> ReadStatusAsync(
        AppDbContext db,
        string key,
        string service,
        string label,
        bool configured,
        string defaultState,
        CancellationToken cancellationToken)
    {
        var stored = await db.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(setting => setting.SettingKey == key, cancellationToken);

        if (stored is null || string.IsNullOrWhiteSpace(stored.Value))
        {
            return new PlatformServiceStatusDto(
                service,
                label,
                defaultState,
                DefaultMessage(defaultState),
                null,
                configured);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<StoredServiceStatus>(stored.Value, JsonOptions);
            if (payload is null)
            {
                throw new JsonException("Empty service status payload.");
            }

            return new PlatformServiceStatusDto(
                service,
                label,
                payload.State,
                payload.Message,
                payload.TestedAt,
                payload.State is not ("not_configured" or "not_imported"));
        }
        catch (JsonException)
        {
            return new PlatformServiceStatusDto(
                service,
                label,
                defaultState,
                DefaultMessage(defaultState),
                stored.UpdatedAt,
                configured);
        }
    }

    private static async Task WriteStatusAsync(
        AppDbContext db,
        string key,
        string state,
        string? message,
        DateTimeOffset testedAt,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(
            new StoredServiceStatus(state, message, testedAt),
            JsonOptions);

        var existing = await db.PlatformSettings
            .FirstOrDefaultAsync(setting => setting.SettingKey == key, cancellationToken);

        if (existing is null)
        {
            db.PlatformSettings.Add(new PlatformSetting
            {
                SettingKey = key,
                Value = payload,
                UpdatedAt = testedAt,
            });
            return;
        }

        existing.Value = payload;
        existing.UpdatedAt = testedAt;
    }

    private static async Task<long> ScalarLongAsync(
        AppDbContext db,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt64(result);
    }

    private static async Task<string?> ScalarStringAsync(
        AppDbContext db,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            return null;
        }

        var value = Convert.ToString(result);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? DefaultMessage(string state) =>
        state switch
        {
            "not_configured" => "Credentials are not configured.",
            "never_tested" => "Configured but not tested yet.",
            "not_imported" => "No WinningCat catalog in the database.",
            _ => null,
        };

    private static string FormatLastImportSuffix(string? lastImportAt) =>
        string.IsNullOrWhiteSpace(lastImportAt) ? string.Empty : $" Last import: {lastImportAt}.";

    private sealed record StoredServiceStatus(
        string State,
        string? Message,
        DateTimeOffset? TestedAt);
}
