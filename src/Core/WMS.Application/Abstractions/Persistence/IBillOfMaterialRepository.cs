using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IBillOfMaterialRepository
{
    Task AddAsync(BillOfMaterial bom, CancellationToken cancellationToken);
    Task<BillOfMaterial?> GetByOutputMaterialIdAsync(Guid outputMaterialId, CancellationToken cancellationToken);

    void Remove(BillOfMaterial bom);
}