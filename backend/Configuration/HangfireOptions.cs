namespace backend.Configuration;

public static class HangfireOptions
{
    /// <summary>
    /// Default: 04:00 UTC daily. Override with tokenmix_pricing_sync_cron (Hangfire CRON).
    /// </summary>
    public static string ReadPricingSyncCron()
    {
        var raw = Environment.GetEnvironmentVariable("tokenmix_pricing_sync_cron")
            ?? Environment.GetEnvironmentVariable("TOKENMIX_PRICING_SYNC_CRON");

        return string.IsNullOrWhiteSpace(raw) ? "0 4 * * *" : raw.Trim();
    }
}
