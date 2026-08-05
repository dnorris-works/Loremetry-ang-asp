using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class CanopyPricingSyncService
{
    public async Task<CanopyPricingSyncResultDto> SyncPlansAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            var syncedAt = DateTimeOffset.UtcNow;

            foreach (var seed in CanopyPricingCatalog.Plans)
            {
                var existing = await db.CanopyPricingPlans
                    .FirstOrDefaultAsync(plan => plan.Id == seed.Id, cancellationToken);

                if (existing is null)
                {
                    existing = new CanopyPricingPlan { Id = seed.Id };
                    db.CanopyPricingPlans.Add(existing);
                }

                existing.DisplayName = seed.DisplayName;
                existing.MonthlyFeeUsd = seed.MonthlyFeeUsd;
                existing.MonthlyRequestAllowance = seed.MonthlyRequestAllowance;
                existing.OveragePricePerRequest = seed.OveragePricePerRequest;
                existing.SortOrder = seed.SortOrder;
                existing.SyncedAt = syncedAt;
            }

            await db.SaveChangesAsync(cancellationToken);

            return new CanopyPricingSyncResultDto(
                true,
                CanopyPricingCatalog.Plans.Count,
                syncedAt,
                null);
        }
        catch (Exception exception)
        {
            return new CanopyPricingSyncResultDto(
                false,
                0,
                DateTimeOffset.UtcNow,
                exception.Message);
        }
    }
}
