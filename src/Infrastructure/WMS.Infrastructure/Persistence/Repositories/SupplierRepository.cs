using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.WarehouseSetup.Queries;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class SupplierRepository(WmsDbContext context) : ISupplierRepository
{
    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken)
    {
        context.Suppliers.Add(supplier);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Suppliers
            .AsNoTracking()
            .Include(s => s.Address)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Suppliers
            .Include(s => s.Address)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public void Remove(Supplier supplier)
    {
        context.Suppliers.Remove(supplier);
    }

    public async Task<Application.Common.Models.PagedResult<SupplierDto>> GetPagedListAsync(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.ContactName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortExpression = $"{request.SortBy} {request.SortDirection ?? "asc"}";
            query = query.OrderBy(sortExpression);
        }
        else
        {
            query = query.OrderBy(s => s.Name);
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.Phone,
                s.Address.City,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);

        return new Application.Common.Models.PagedResult<SupplierDto> { Items = items, TotalCount = totalCount };
    }
}