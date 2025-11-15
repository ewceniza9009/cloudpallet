using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Repositories;

public class RateRepository(WmsDbContext context) : IRateRepository
{
    public async Task<Rate?> GetRateForAccountAsync(Guid accountId, ServiceType serviceType, RateUom uom, string? tier, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        tier ??= string.Empty;

        // 1. Try to find the rate for the specific account
        var specificRate = await context.Rates.FirstOrDefaultAsync(r =>
                r.AccountId == accountId &&
                r.ServiceType == serviceType &&
                r.Uom == uom &&
                r.Tier == tier &&
                r.IsActive &&
                r.EffectiveStartDate <= now &&
                (r.EffectiveEndDate == null || r.EffectiveEndDate >= now),
            cancellationToken);

        if (specificRate != null)
        {
            return specificRate;
        }

        // 2. If not found, fall back to the "Default Account"
        var defaultAccId = Guid.Empty;

        // Check for AccountId being NULL (from old data) OR Guid.Empty (the correct default)
        return await context.Rates.FirstOrDefaultAsync(r =>
                (r.AccountId == defaultAccId || r.AccountId == null) &&
                r.ServiceType == serviceType &&
                r.Uom == uom &&
                r.Tier == tier &&
                r.IsActive &&
                r.EffectiveStartDate <= now &&
                (r.EffectiveEndDate == null || r.EffectiveEndDate >= now),
            cancellationToken);
    }

    public async Task<IEnumerable<Rate>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Rates
            .AsNoTracking()
            .OrderBy(r => r.AccountId)
            .ThenBy(r => r.ServiceType)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Rate rate, CancellationToken cancellationToken)
    {
        context.Rates.Add(rate);
        return Task.CompletedTask;
    }

    public async Task<Rate?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Rates.FindAsync([id], cancellationToken);
    }

    public void Remove(Rate rate)
    {
        context.Rates.Remove(rate);
    }
}