using backend.Configuration;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class PlatformSettingsService
{
    public static async Task SeedFromEnvironmentAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var key in PlatformSettingKeys.All)
        {
            var envValue = PlatformSettingKeys.ReadFromEnvironment(key);
            if (string.IsNullOrEmpty(envValue))
            {
                envValue = PlatformSettingKeys.DefaultValue(key);
            }

            var existing = await db.PlatformSettings
                .FirstOrDefaultAsync(setting => setting.SettingKey == key, cancellationToken);

            if (existing is null)
            {
                db.PlatformSettings.Add(new PlatformSetting
                {
                    SettingKey = key,
                    Value = envValue,
                    UpdatedAt = now,
                });
            }
            else if (string.IsNullOrWhiteSpace(existing.Value) && !string.IsNullOrWhiteSpace(envValue))
            {
                existing.Value = envValue;
                existing.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<PlatformSettingsDto> GetAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var values = await db.PlatformSettings
            .AsNoTracking()
            .ToDictionaryAsync(setting => setting.SettingKey, setting => setting.Value, cancellationToken);

        string Value(string key) =>
            values.TryGetValue(key, out var value) ? value : PlatformSettingKeys.DefaultValue(key);

        var anthropic = Value(PlatformSettingKeys.AnthropicApiKey);
        var tokenmix = Value(PlatformSettingKeys.TokenmixApiKey);
        var canopy = Value(PlatformSettingKeys.CanopyApiKey);
        var login = Value(PlatformSettingKeys.DataForSeoLogin);
        var password = Value(PlatformSettingKeys.DataForSeoPassword);
        var provider = Value(PlatformSettingKeys.DefaultProvider);
        var model = Value(PlatformSettingKeys.DefaultModel);

        return new PlatformSettingsDto(
            anthropic,
            tokenmix,
            canopy,
            login,
            password,
            provider,
            model,
            !string.IsNullOrWhiteSpace(anthropic),
            !string.IsNullOrWhiteSpace(tokenmix),
            !string.IsNullOrWhiteSpace(canopy),
            !string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(password),
            []);
    }

    public static async Task<PlatformSettingsDto> GetWithServiceStatusesAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await GetAsync(db, cancellationToken);
        var serviceStatuses = await PlatformServiceStatusService.GetAllAsync(db, settings, cancellationToken);

        return settings with { ServiceStatuses = serviceStatuses };
    }

    public static async Task<PlatformSettingsDto> UpdateAsync(
        UpdatePlatformSettingsRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var provider = request.DefaultProvider.Trim();
        if (provider is not ("tokenmix" or "anthropic"))
        {
            throw new InvalidOperationException("default_provider must be tokenmix or anthropic.");
        }

        Dictionary<string, string> updates = new()
        {
            [PlatformSettingKeys.AnthropicApiKey] = request.AnthropicApiKey.Trim(),
            [PlatformSettingKeys.TokenmixApiKey] = request.TokenmixApiKey.Trim(),
            [PlatformSettingKeys.CanopyApiKey] = request.CanopyApiKey.Trim(),
            [PlatformSettingKeys.DataForSeoLogin] = request.DataForSeoLogin.Trim(),
            [PlatformSettingKeys.DataForSeoPassword] = request.DataForSeoPassword.Trim(),
            [PlatformSettingKeys.DefaultProvider] = provider,
            [PlatformSettingKeys.DefaultModel] = request.DefaultModel.Trim(),
        };

        var now = DateTimeOffset.UtcNow;

        foreach (var (key, value) in updates)
        {
            var setting = await db.PlatformSettings
                .FirstOrDefaultAsync(item => item.SettingKey == key, cancellationToken);

            if (setting is null)
            {
                db.PlatformSettings.Add(new PlatformSetting
                {
                    SettingKey = key,
                    Value = value,
                    UpdatedAt = now,
                });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetWithServiceStatusesAsync(db, cancellationToken);
    }
}
