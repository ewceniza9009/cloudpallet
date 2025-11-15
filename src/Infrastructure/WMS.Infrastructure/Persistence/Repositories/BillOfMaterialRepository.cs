using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class BillOfMaterialRepository(WmsDbContext context) : IBillOfMaterialRepository
{
    public Task AddAsync(BillOfMaterial bom, CancellationToken cancellationToken)
    {
        context.BillOfMaterials.Add(bom);
        return Task.CompletedTask;
    }

    public async Task<BillOfMaterial?> GetByOutputMaterialIdAsync(Guid outputMaterialId, CancellationToken cancellationToken)
    {
        return await context.BillOfMaterials
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.OutputMaterialId == outputMaterialId, cancellationToken);
    }

    public void Remove(BillOfMaterial bom)
    {
        context.BillOfMaterials.Remove(bom);
    }
}