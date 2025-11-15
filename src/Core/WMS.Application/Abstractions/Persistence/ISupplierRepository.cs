using WMS.Application.Common.Models;   
using WMS.Application.Features.WarehouseSetup.Queries;   
using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface ISupplierRepository
{
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken);
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken);       
    void Remove(Supplier supplier);

    Task<PagedResult<SupplierDto>> GetPagedListAsync(GetSuppliersQuery query, CancellationToken cancellationToken);
}