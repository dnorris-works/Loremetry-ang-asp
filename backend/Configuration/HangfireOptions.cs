namespace backend.Configuration;

public static class HangfireOptions
{
    /// <summary>
    /// Default: Sundays at 09:00 UTC (~4 AM US Eastern, ~1 AM Pacific).
    /// Hangfire CRON is UTC-only; override with tokenmix_pricing_sync_cron if needed.
    /// </summary>
    public static string ReadPricingSyncCron()
    {
        var raw = Environment.GetEnvironmentVariable("tokenmix_pricing_sync_cron")
            ?? Environment.GetEnvironmentVariable("TOKENMIX_PRICING_SYNC_CRON");

        return string.IsNullOrWhiteSpace(raw) ? "0 9 * * 0" : raw.Trim();
    }
}
