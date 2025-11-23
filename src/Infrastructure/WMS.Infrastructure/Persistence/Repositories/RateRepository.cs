using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
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

    public async Task<Application.Common.Models.PagedResult<Application.Features.Admin.Queries.RateDto>> GetPagedListAsync(Application.Features.Admin.Queries.GetRatesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Rates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            // Note: ServiceType and Uom are enums, so we might need to filter client-side or use string conversion if supported by DB provider.
            // For now, let's assume we filter by Tier or Account ID (if we had account name join).
            // Since we don't have Account Name join easily here without Include, let's filter by Tier.
            query = query.Where(r => r.Tier.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortColumn = request.SortBy.ToLowerInvariant() switch
            {
                "servicetype" => "ServiceType",
                "uom" => "Uom",
                "value" => "Value",
                "tier" => "Tier",
                "effectivestartdate" => "EffectiveStartDate",
                "isactive" => "IsActive",
                _ => "ServiceType"
            };
            var sortDirection = request.SortDirection?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortColumn} {sortDirection}");
        }
        else
        {
            query = query.OrderBy(r => r.ServiceType);
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(rate => new Application.Features.Admin.Queries.RateDto(
                rate.Id,
                rate.AccountId,
                rate.ServiceType,
                rate.Uom,
                rate.Value,
                rate.Tier,
                rate.EffectiveStartDate,
                rate.EffectiveEndDate,
                rate.IsActive
            ))
            .ToListAsync(cancellationToken);

        return new Application.Common.Models.PagedResult<Application.Features.Admin.Queries.RateDto> { Items = items, TotalCount = totalCount };
    }
}