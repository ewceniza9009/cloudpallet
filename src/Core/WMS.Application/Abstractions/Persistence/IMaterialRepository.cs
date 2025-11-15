using WMS.Application.Common.Models;     
using WMS.Application.Features.Admin.Queries;       
using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IMaterialRepository
{
    Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Material>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task AddAsync(Material material, CancellationToken cancellationToken);
    Task<IEnumerable<Material>> GetAllAsync(CancellationToken cancellationToken);
    void Remove(Material material);

    Task<PagedResult<MaterialDetailDto>> GetPagedListAsync(GetMaterialsQuery request, CancellationToken cancellationToken);
}