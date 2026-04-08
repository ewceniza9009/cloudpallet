using WMS.Application.Common.Models;
using WMS.Application.Features.Lookups;

namespace WMS.Application.Abstractions.Services;

public interface ILookupService
{
    Task<IEnumerable<WarehouseDto>> GetWarehousesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PalletTypeDto>> GetPalletTypesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DockDto>> GetDocksAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LookupDto>> GetMaterialCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LookupDto>> GetUomsAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<MaterialDto>> SearchMaterialsAsync(string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AppointmentDto>> GetActiveAppointmentsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<YardSpotDto>> GetAvailableYardSpotsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TruckDto>> GetTrucksAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LocationDto>> GetAvailableStorageLocationsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MaterialDto>> GetPickableMaterialsAsync(Guid warehouseId, Guid accountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AppointmentDto>> GetOutboundAppointmentsAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<RepackableInventoryDto>> GetRepackableInventoryAsync(Guid accountId, Guid? materialId, string? searchTerm, int page, int pageSize, bool includeAllLocations, CancellationToken cancellationToken = default);
    Task<string> GetBarcodeHtmlAsync(string barcodeText, string type, decimal? quantity, CancellationToken cancellationToken = default);
    Task<string> DiagnoseBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
}
