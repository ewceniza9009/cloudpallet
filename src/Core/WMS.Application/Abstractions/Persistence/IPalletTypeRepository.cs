using WMS.Application.Common.Models;   
using WMS.Application.Features.WarehouseSetup.Queries;     
using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IPalletTypeRepository
{
    Task AddAsync(PalletType palletType, CancellationToken cancellationToken);
    Task<IEnumerable<PalletType>> GetAllActiveAsync(CancellationToken cancellationToken);
    Task<PalletType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<PalletTypeDto>> GetPagedListAsync(GetPalletTypesQuery request, CancellationToken cancellationToken);
    void Remove(PalletType palletType);
}