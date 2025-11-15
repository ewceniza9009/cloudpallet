using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;    
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;   
using WMS.Application.Features.WarehouseSetup.Queries;     
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class PalletTypeRepository(WmsDbContext context) : IPalletTypeRepository
{
    public Task AddAsync(PalletType palletType, CancellationToken cancellationToken)
    {
        context.Set<PalletType>().Add(palletType);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<PalletType>> GetAllActiveAsync(CancellationToken cancellationToken)
    {
        return await context.Set<PalletType>()
            .AsNoTracking()
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PalletType?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Set<PalletType>().FindAsync([id], cancellationToken);
    }

    public async Task<Application.Common.Models.PagedResult<PalletTypeDto>> GetPagedListAsync(GetPalletTypesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<PalletType>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(pt => pt.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortColumn = request.SortBy.ToLowerInvariant() switch
            {
                "name" => "Name",
                "tareweight" => "TareWeight",
                "isactive" => "IsActive",
                _ => "Name"   
            };
            var sortDirection = request.SortDirection?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortColumn} {sortDirection}");
        }
        else
        {
            query = query.OrderBy(pt => pt.Name);   
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(pt => new PalletTypeDto(    
                pt.Id,
                pt.Name,
                pt.TareWeight,
                pt.Length,
                pt.Width,
                pt.Height,
                pt.IsActive
            ))
            .ToListAsync(cancellationToken);

        return new Application.Common.Models.PagedResult<PalletTypeDto> { Items = items, TotalCount = totalCount };
    }

    public void Remove(PalletType palletType)
    {
        context.Set<PalletType>().Remove(palletType);
    }
}