using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;
using WMS.Application.Features.WarehouseSetup.Queries;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class UnitOfMeasureRepository(WmsDbContext context) : IUnitOfMeasureRepository
{
    public Task AddAsync(UnitOfMeasure uom, CancellationToken cancellationToken)
    {
        context.UnitsOfMeasure.Add(uom);
        return Task.CompletedTask;
    }

    public async Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.UnitsOfMeasure.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Application.Common.Models.PagedResult<UnitOfMeasureDto>> GetPagedListAsync(GetUnitOfMeasuresQuery request, CancellationToken cancellationToken)
    {
        var query = context.UnitsOfMeasure.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(uom => uom.Name.ToLower().Contains(term) || uom.Symbol.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortExpression = $"{request.SortBy} {request.SortDirection ?? "asc"}";
            query = query.OrderBy(sortExpression);
        }
        else
        {
            query = query.OrderBy(uom => uom.Name);   
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(uom => new UnitOfMeasureDto(
                uom.Id,
                uom.Name,
                uom.Symbol
            ))
            .ToListAsync(cancellationToken);

        return new Application.Common.Models.PagedResult<UnitOfMeasureDto> { Items = items, TotalCount = totalCount };
    }

    public void Remove(UnitOfMeasure uom)
    {
        context.UnitsOfMeasure.Remove(uom);
    }
}