using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;      
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;     
using WMS.Application.Features.Admin.Queries;       
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class MaterialRepository(WmsDbContext context) : IMaterialRepository
{
    public async Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Materials.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<Material>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        return await context.Materials
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Material material, CancellationToken cancellationToken)
    {
        context.Materials.Add(material);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Material>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Materials
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public void Remove(Material material)
    {
        context.Materials.Remove(material);
    }

    public async Task<Application.Common.Models.PagedResult<MaterialDetailDto>> GetPagedListAsync(GetMaterialsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(term) || m.Sku.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortColumn = request.SortBy.ToLowerInvariant() switch
            {
                "name" => "Name",       
                "sku" => "Sku",      
                "materialtype" => "MaterialType",
                "requiredtempzone" => "RequiredTempZone",
                "costperunit" => "CostPerUnit",
                "baseweight" => "BaseWeight",
                "isactive" => "IsActive",
                _ => "Name"   
            };
            var sortDirection = request.SortDirection?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortColumn} {sortDirection}");
        }
        else
        {
            query = query.OrderBy(m => m.Name);   
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MaterialDetailDto(     
                m.Id,
                m.Name,
                m.Sku,
                m.Description,
                m.CategoryId,
                m.UomId,
                m.RequiredTempZone.ToString(),
                m.BaseWeight,
                m.CostPerUnit,
                m.MaterialType,
                m.Perishable,
                m.ShelfLifeDays,
                m.IsHazardous,
                m.Gs1BarcodePrefix,
                m.IsActive,
                m.DefaultBarcodeFormat,
                m.DimensionsLength,
                m.DimensionsWidth,
                m.DimensionsHeight,
                m.MinStockLevel,
                m.MaxStockLevel,
                m.PackageTareWeightPerUom
            ))
            .ToListAsync(cancellationToken);

        return new Application.Common.Models.PagedResult<MaterialDetailDto> { Items = items, TotalCount = totalCount };
    }
}