// ---- File: src/Infrastructure/WMS.Infrastructure/Persistence/Repositories/ReceivingTransactionRepository.cs ----

using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Queries;
using WMS.Domain.Entities.Transaction;
using System.Text;
using WMS.Application.Common.Models; // Required for StringBuilder

namespace WMS.Infrastructure.Persistence.Repositories;

public class ReceivingTransactionRepository(WmsDbContext context) : IReceivingTransactionRepository
{
    public Task AddAsync(Receiving receiving, CancellationToken cancellationToken)
    {
        context.Set<Receiving>().Add(receiving);
        return Task.CompletedTask;
    }

    public async Task<Receiving?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Set<Receiving>()
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<Receiving?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Set<Receiving>()
            .Include(r => r.Pallets)
                .ThenInclude(p => p.PalletType)
            .Include(r => r.Pallets)
                .ThenInclude(p => p.Lines)
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<Receiving?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return await context.Receivings
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, cancellationToken);
    }

    public async Task<Guid?> GetWarehouseIdForReceivingAsync(Guid receivingId, CancellationToken cancellationToken)
    {
        var query = from r in context.Receivings
                    where r.Id == receivingId
                    join appt in context.DockAppointments on r.AppointmentId equals appt.Id
                    join dock in context.Warehouses.SelectMany(w => w.Docks) on appt.DockId equals dock.Id
                    select dock.WarehouseId;

        var warehouseId = await query.FirstOrDefaultAsync(cancellationToken);
        return warehouseId == Guid.Empty ? null : warehouseId;
    }

    public async Task<Guid?> GetReceivingIdForPalletAsync(Guid palletId, CancellationToken cancellationToken)
    {
        var pallet = await context.Set<Pallet>().FindAsync([palletId], cancellationToken);
        return pallet?.ReceivingId;
    }

    public async Task<int> GetPalletCountAsync(Guid receivingId, CancellationToken cancellationToken)
    {
        return await context.Set<Pallet>().CountAsync(p => p.ReceivingId == receivingId, cancellationToken);
    }

    public Task AddPalletAsync(Pallet pallet, CancellationToken cancellationToken)
    {
        context.Set<Pallet>().Add(pallet);
        return Task.CompletedTask;
    }

    public Task AddPalletLineAsync(PalletLine palletLine, CancellationToken cancellationToken)
    {
        context.Set<PalletLine>().Add(palletLine);
        return Task.CompletedTask;
    }

    public Task UpdatePalletLineAsync(PalletLine palletLine, CancellationToken cancellationToken)
    {
        context.Set<PalletLine>().Update(palletLine);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<ReceivingSessionDto>> GetReceivingSessionsByWarehouseAsync(GetReceivingSessionsQuery request, CancellationToken cancellationToken)
    {
        // 1. Base Query with Filters
        var query = context.Receivings
            .AsNoTracking()
            .Where(r => r.Appointment.Dock.WarehouseId == request.WarehouseId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(r => r.Supplier.Name.Contains(term) ||
                                     (r.Appointment.Truck != null && r.Appointment.Truck.LicensePlate.Contains(term)));
        }

        if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
        {
            query = query.Where(r => r.Timestamp.Date == parsedDate.Date);
        }

        // 2. Get Total Count (Before Paging)
        var totalCount = await query.CountAsync(cancellationToken);

        // 3. Apply Sorting and Pagination
        var items = await query
            .OrderByDescending(r => r.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReceivingSessionDto
            {
                ReceivingId = r.Id,
                SupplierName = r.Supplier.Name,
                LicensePlate = r.Appointment.Truck != null ? r.Appointment.Truck.LicensePlate : "N/A",
                Status = r.Status,
                Timestamp = r.Timestamp,
                PalletCount = r.TotalPallets
            })
            .ToListAsync(cancellationToken);

        // 4. Return Paged Result
        return new PagedResult<ReceivingSessionDto>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<Pallet?> GetPalletWithLinesByIdAsync(Guid palletId, CancellationToken cancellationToken)
    {
        return await context.Set<Pallet>()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == palletId, cancellationToken);
    }

    public async Task<PalletLine?> GetPalletLineByIdAsync(Guid palletLineId, CancellationToken cancellationToken)
    {
        return await context.Set<PalletLine>()
            .FirstOrDefaultAsync(pl => pl.Id == palletLineId, cancellationToken);
    }

    public void RemovePallet(Pallet pallet)
    {
        context.Set<Pallet>().Remove(pallet);
    }

    public void RemovePalletLine(PalletLine palletLine)
    {
        context.Set<PalletLine>().Remove(palletLine);
    }

    public async Task<IEnumerable<Receiving>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await context.Receivings
            .AsNoTracking()
            .Include(r => r.Pallets)
            .ThenInclude(p => p.Lines)
            .Where(r => r.AccountId == accountId && r.Timestamp >= startDate && r.Timestamp < endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PalletMovementDto>> GetPalletHistoryAsync(string palletBarcode, CancellationToken cancellationToken)
    {
        var pallet = await context.Pallets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == palletBarcode, cancellationToken);

        if (pallet is null)
        {
            return Enumerable.Empty<PalletMovementDto>();
        }

        var movements = new List<PalletMovementDto>();

        // Event: Received (Unchanged)
        var receivingTx = await context.Receivings
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == pallet.ReceivingId, cancellationToken);
        if (receivingTx != null)
        {
            movements.Add(new PalletMovementDto
            {
                EventType = "Received",
                Timestamp = receivingTx.Timestamp,
                Location = "Receiving Dock",
                Details = $"Received from supplier: {receivingTx.Supplier.Name}"
            });
        }

        // Event: Created from Transfer (Unchanged)
        var transferInTx = await context.ItemTransferTransactions
            .AsNoTracking()
            .Include(t => t.SourceInventory).ThenInclude(si => si.Pallet)
            .Include(t => t.SourceInventory).ThenInclude(si => si.Material)
            .FirstOrDefaultAsync(t => t.NewDestinationPalletId == pallet.Id, cancellationToken);

        if (transferInTx != null)
        {
            movements.Add(new PalletMovementDto
            {
                EventType = "Created from Transfer",
                Timestamp = transferInTx.Timestamp,
                Location = "Warehouse Floor",
                Details = $"Created from transfer of {transferInTx.QuantityTransferred:N0} '{transferInTx.SourceInventory.Material.Name}' from pallet {transferInTx.SourceInventory.Pallet.Barcode}"
            });
        }

        // Event: Put Away (Unchanged)
        var putawayTx = await context.PutawayTransactions
            .Include(pt => pt.Location)
            .FirstOrDefaultAsync(pt => pt.PalletId == pallet.Id, cancellationToken);
        if (putawayTx != null)
        {
            movements.Add(new PalletMovementDto
            {
                EventType = "Put Away",
                Timestamp = putawayTx.Timestamp,
                Location = $"BAY-{putawayTx.Location.Bay} / {putawayTx.Location.Barcode}",
                Details = $"Moved from Staging to Storage"
            });
        }

        // Event: Pallet Transferred (Unchanged)
        var transferTxs = await context.TransferTransactions
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Where(t => t.PalletId == pallet.Id)
            .ToListAsync(cancellationToken);
        movements.AddRange(transferTxs.Select(tx => new PalletMovementDto
        {
            EventType = "Transferred",
            Timestamp = tx.Timestamp,
            Location = $"BAY-{tx.ToLocation.Bay} / {tx.ToLocation.Barcode}",
            Details = $"Moved from BAY-{tx.FromLocation.Bay} / {tx.FromLocation.Barcode} to BAY-{tx.ToLocation.Bay} / {tx.ToLocation.Barcode}"
        }));

        // Event: Items Transferred Out (Unchanged)
        var transferOutTxs = await context.ItemTransferTransactions
            .AsNoTracking()
            .Include(t => t.NewDestinationPallet)
            .Include(t => t.SourceInventory).ThenInclude(si => si.Material)
            .Where(t => t.SourceInventory.PalletId == pallet.Id)
            .ToListAsync(cancellationToken);
        movements.AddRange(transferOutTxs.Select(tx => new PalletMovementDto
        {
            EventType = "Items Transferred Out",
            Timestamp = tx.Timestamp,
            Location = "Warehouse Floor",
            Details = $"Transferred {tx.QuantityTransferred:N0} of '{tx.SourceInventory.Material.Name}' to new pallet {tx.NewDestinationPallet.Barcode}"
        }));

        // --- START MODIFICATION: Fix VAS Query ---
        // The original query was correct, it was my analysis that was wrong.
        // This query fetches ALL VAS transactions for the pallet.
        var vasTxs = await context.VASTransactions
            .Where(vt => vt.PalletId == pallet.Id)
            .ToListAsync(cancellationToken);
        // --- END MODIFICATION ---

        movements.AddRange(vasTxs.Select(tx => new PalletMovementDto
        {
            EventType = tx.ServiceType.ToString(), // Will now show "Labeling", "Fumigation", etc.
            Timestamp = tx.Timestamp,
            Location = "VAS Area",
            Details = tx.Description
        }));

        // Event: Picked & Shipped (Unchanged)
        var pickTxs = await context.PickTransactions
            .Include(p => p.WithdrawalTransactions)
            .Where(p => p.PalletId == pallet.Id)
            .ToListAsync(cancellationToken);

        foreach (var pick in pickTxs)
        {
            movements.Add(new PalletMovementDto
            {
                EventType = "Picked",
                Timestamp = pick.Timestamp,
                Location = "Staging Area",
                Details = $"Picked {pick.Quantity:N0} units for shipment"
            });
            var withdrawal = pick.WithdrawalTransactions.FirstOrDefault();
            if (withdrawal != null)
            {
                movements.Add(new PalletMovementDto
                {
                    EventType = "Shipped",
                    Timestamp = withdrawal.Timestamp,
                    Location = "Shipping Dock",
                    Details = $"Shipped via appointment {withdrawal.AppointmentId}"
                });
            }
        }

        // Finally, sort all collected events by date
        return movements.OrderBy(m => m.Timestamp);
    }

    public void Remove(Receiving receiving)
    {
        context.Receivings.Remove(receiving);
    }
}