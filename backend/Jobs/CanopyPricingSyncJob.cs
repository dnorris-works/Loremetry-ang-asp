using backend.Data;
using backend.Services;
using Hangfire;

namespace backend.Jobs;

public sealed class CanopyPricingSyncJob(
    IServiceScopeFactory scopeFactory,
    ILogger<CanopyPricingSyncJob> logger)
{
    public const string JobId = "canopy-pricing-sync";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<CanopyPricingSyncService>();

        var result = await syncService.SyncPlansAsync(db, cancellationToken);

        if (!result.Success)
        {
            logger.LogWarning(
                "Canopy pricing sync failed: {Error}",
                result.Error ?? "Unknown error");
            throw new InvalidOperationException(result.Error ?? "Canopy pricing sync failed.");
        }

        logger.LogInformation(
            "Canopy pricing synced: {Plans} plans at {SyncedAt}",
            result.Plans,
            result.SyncedAt);
    }
}
