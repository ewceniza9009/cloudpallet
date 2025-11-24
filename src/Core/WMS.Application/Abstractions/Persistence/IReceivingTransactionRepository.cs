using WMS.Application.Common.Models;
using WMS.Application.Features.Inventory.Queries;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IReceivingTransactionRepository
{
    Task AddAsync(Receiving receiving, CancellationToken cancellationToken);
    Task<Receiving?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Receiving?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid?> GetWarehouseIdForReceivingAsync(Guid receivingId, CancellationToken cancellationToken);
    Task<IEnumerable<Receiving>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<Guid?> GetReceivingIdForPalletAsync(Guid palletId, CancellationToken cancellationToken);
    Task<int> GetPalletCountAsync(Guid receivingId, CancellationToken cancellationToken);
    Task AddPalletAsync(Pallet pallet, CancellationToken cancellationToken);
    Task AddPalletLineAsync(PalletLine palletLine, CancellationToken cancellationToken);
    Task UpdatePalletLineAsync(PalletLine palletLine, CancellationToken cancellationToken);

    Task<PagedResult<ReceivingSessionDto>> GetReceivingSessionsByWarehouseAsync(GetReceivingSessionsQuery request, CancellationToken cancellationToken);
    Task<Pallet?> GetPalletWithLinesByIdAsync(Guid palletId, CancellationToken cancellationToken);
    Task<PalletLine?> GetPalletLineByIdAsync(Guid palletLineId, CancellationToken cancellationToken);
    void RemovePallet(Pallet pallet);
    void RemovePalletLine(PalletLine palletLine);
    Task<Receiving?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken);
    Task<IEnumerable<PalletMovementDto>> GetPalletHistoryAsync(string palletBarcode, CancellationToken cancellationToken);
    void Remove(Receiving receiving);
}