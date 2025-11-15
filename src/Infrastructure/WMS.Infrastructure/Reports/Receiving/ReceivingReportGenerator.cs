using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.Receiving;

public class ReceivingReportGenerator(WmsDbContext context)
{
    public async Task<(List<ReceivingReportItem> Data, Dictionary<Guid, string> UomMap)> GenerateDataAsync(ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.PalletLines.AsNoTracking()
            .Where(l => l.Pallet.Receiving.Timestamp >= filters.StartDate && l.Pallet.Receiving.Timestamp <= filters.EndDate)
            .Where(l => !filters.AccountId.HasValue || l.AccountId == filters.AccountId);

        if (filters.SupplierId.HasValue)
        {
            query = query.Where(l => l.Pallet.Receiving.SupplierId == filters.SupplierId);
        }
        if (filters.MaterialId.HasValue)
        {
            query = query.Where(l => l.MaterialId == filters.MaterialId);
        }
        if (filters.UserId.HasValue)
        {
            query = query.Where(l => l.CreatedBy == filters.UserId);
        }

        var receivingEntries = await query
            .Include(l => l.Material)
            .Include(l => l.Pallet)
                .ThenInclude(p => p.Receiving)
                    .ThenInclude(r => r.Supplier)
            .OrderBy(l => l.Pallet.Receiving.Timestamp)
            .Select(l => new ReceivingReportItem
            {
                ReceivingId = l.Pallet.Receiving.Id,
                Timestamp = l.Pallet.Receiving.Timestamp,
                SupplierName = l.Pallet.Receiving.Supplier.Name,
                MaterialId = l.MaterialId,
                MaterialName = l.Material.Name,
                MaterialSKU = l.Material.Sku,
                Quantity = l.Quantity,
                UomId = l.Material.UomId,
                WeightKg = l.Weight,
            })
            .ToListAsync(cancellationToken);

        var uomMap = new Dictionary<Guid, string>();
        var uomIds = receivingEntries.Select(r => r.UomId).Distinct().ToList();
        if (uomIds.Any())
        {
            uomMap = await context.UnitsOfMeasure
                .Where(u => uomIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

            foreach (var entry in receivingEntries)
            {
                entry.Uom = uomMap.GetValueOrDefault(entry.UomId, "?");
            }
        }

        return (receivingEntries, uomMap);
    }
}