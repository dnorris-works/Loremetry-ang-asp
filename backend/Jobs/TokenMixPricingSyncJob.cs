using backend.Data;
using backend.Services;
using Hangfire;

namespace backend.Jobs;

public sealed class TokenMixPricingSyncJob(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenMixPricingSyncJob> logger)
{
    public const string JobId = "tokenmix-pricing-sync";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<TokenMixPricingSyncService>();

        var result = await syncService.SyncChatModelsAsync(db, cancellationToken);

        if (!result.Success)
        {
            logger.LogWarning(
                "TokenMix pricing sync failed: {Error}",
                result.Error ?? "Unknown error");
            throw new InvalidOperationException(result.Error ?? "TokenMix pricing sync failed.");
        }

        logger.LogInformation(
            "TokenMix pricing synced: {ChatModels} chat models at {SyncedAt}",
            result.ChatModels,
            result.SyncedAt);
    }
}
