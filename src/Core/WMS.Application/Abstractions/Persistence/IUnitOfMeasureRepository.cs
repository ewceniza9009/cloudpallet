using WMS.Application.Common.Models;
using WMS.Application.Features.WarehouseSetup.Queries;
using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IUnitOfMeasureRepository
{
    Task AddAsync(UnitOfMeasure uom, CancellationToken cancellationToken);
    Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<UnitOfMeasureDto>> GetPagedListAsync(GetUnitOfMeasuresQuery query, CancellationToken cancellationToken);
    void Remove(UnitOfMeasure uom);
}