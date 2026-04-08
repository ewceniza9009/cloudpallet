using Microsoft.EntityFrameworkCore;
using System.Text;
using WMS.Application.Abstractions.Caching;
using WMS.Application.Abstractions.Services;
using WMS.Application.Common.Models;
using WMS.Application.Features.Lookups;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class LookupService(WmsDbContext context, ICacheService cache) : ILookupService
{
    private const string CachePrefix = "lookups:";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(1);
    private static readonly TimeSpan ShortExpiry = TimeSpan.FromMinutes(5);

    public async Task<IEnumerable<WarehouseDto>> GetWarehousesAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}warehouses";
        var cached = await cache.GetAsync<IEnumerable<WarehouseDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(w.Id, w.Name))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<PalletTypeDto>> GetPalletTypesAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}pallet-types";
        var cached = await cache.GetAsync<IEnumerable<PalletTypeDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.PalletTypes
            .AsNoTracking()
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.Name)
            .Select(pt => new PalletTypeDto(pt.Id, pt.Name, pt.TareWeight))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<DockDto>> GetDocksAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        string key = $"{CachePrefix}docks:{warehouseId}";
        var cached = await cache.GetAsync<IEnumerable<DockDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await (from dock in context.Docks.AsNoTracking().Where(d => d.WarehouseId == warehouseId)
                          join appt in context.DockAppointments on dock.CurrentAppointmentId equals appt.Id into apptGroup
                          from appt in apptGroup.DefaultIfEmpty()
                          join truck in context.Trucks on appt.TruckId equals truck.Id into truckGroup
                          from truck in truckGroup.DefaultIfEmpty()
                          join carrier in context.Carriers on truck.CarrierId equals carrier.Id into carrierGroup
                          from carrier in carrierGroup.DefaultIfEmpty()
                          orderby dock.Name
                          select new DockDto(
                            dock.Id,
                            dock.Name,
                            dock.CurrentAppointmentId == null,
                            dock.CurrentAppointmentId,
                            truck == null ? null : truck.LicensePlate,
                            carrier == null ? null : carrier.Name,
                            appt == null ? null : (DateTime?)appt.StartDateTime
                          )).ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, ShortExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}suppliers";
        var cached = await cache.GetAsync<IEnumerable<SupplierDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}materials";
        var cached = await cache.GetAsync<IEnumerable<MaterialDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.Materials
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new MaterialDto(m.Id, m.Name, m.Sku, m.MaterialType.ToString()))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<LookupDto>> GetMaterialCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}material-categories";
        var cached = await cache.GetAsync<IEnumerable<LookupDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.MaterialCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<LookupDto>> GetUomsAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}uoms";
        var cached = await cache.GetAsync<IEnumerable<LookupDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.UnitsOfMeasure
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new LookupDto(u.Id, u.Name))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<PagedResult<MaterialDto>> SearchMaterialsAsync(string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Search results are typically not cached in Redis due to high cardinality of search terms
        var query = context.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(m => m.Name.Contains(searchTerm) || m.Sku.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MaterialDto(m.Id, m.Name, m.Sku, m.MaterialType.ToString()))
            .ToListAsync(cancellationToken);

        return new PagedResult<MaterialDto> { Items = items, TotalCount = totalCount };
    }

    public async Task<IEnumerable<AppointmentDto>> GetActiveAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}active-appointments";
        var cached = await cache.GetAsync<IEnumerable<AppointmentDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.DockAppointments
            .Include(a => a.Truck)
            .AsNoTracking()
            .Where(a => a.Type == AppointmentType.Receiving &&
                  a.StartDateTime > DateTime.UtcNow.AddDays(-1) &&
                  a.Status != AppointmentStatus.Completed &&
                  a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartDateTime)
            .Select(a => new AppointmentDto(
              a.Id,
              (a.Truck != null ? a.Truck.LicensePlate : "N/A"),
              a.StartDateTime))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, ShortExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<YardSpotDto>> GetAvailableYardSpotsAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        string key = $"{CachePrefix}yard-spots:{warehouseId}";
        var cached = await cache.GetAsync<IEnumerable<YardSpotDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.YardSpots
            .AsNoTracking()
            .Where(ys => ys.WarehouseId == warehouseId && ys.Status == YardSpotStatus.Available)
            .OrderBy(ys => ys.SpotNumber)
            .Select(ys => new YardSpotDto(ys.Id, ys.SpotNumber))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, ShortExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}accounts";
        var cached = await cache.GetAsync<IEnumerable<AccountDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .Select(a => new AccountDto(a.Id, a.Name))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<TruckDto>> GetTrucksAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}trucks";
        var cached = await cache.GetAsync<IEnumerable<TruckDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.Trucks
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.LicensePlate)
            .Select(t => new TruckDto(t.Id, t.LicensePlate))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, DefaultExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<LocationDto>> GetAvailableStorageLocationsAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        string key = $"{CachePrefix}available-locations:{warehouseId}";
        var cached = await cache.GetAsync<IEnumerable<LocationDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.Warehouses
            .Where(w => w.Id == warehouseId)
            .SelectMany(w => w.Rooms)
            .SelectMany(r => r.Locations.Select(l => new { Location = l, RoomName = r.Name }))
            .Where(x => x.Location.ZoneType == LocationType.Storage && x.Location.IsEmpty && x.Location.IsActive)
            .OrderBy(x => x.RoomName)
            .ThenBy(x => x.Location.Bay)
            .ThenBy(x => x.Location.Row)
            .ThenBy(x => x.Location.Column)
            .Select(x => new LocationDto(
              x.Location.Id,
              $"{x.RoomName} / {x.Location.Bay} / ({x.Location.Barcode})"))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, ShortExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<MaterialDto>> GetPickableMaterialsAsync(Guid warehouseId, Guid accountId, CancellationToken cancellationToken = default)
    {
        string key = $"{CachePrefix}pickable-materials:{warehouseId}:{accountId}";
        var cached = await cache.GetAsync<IEnumerable<MaterialDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var pickableMaterialIds = await context.Warehouses
            .Where(w => w.Id == warehouseId)
            .SelectMany(w => w.Rooms)
            .SelectMany(r => r.Locations)
            .Where(l => l.ZoneType == LocationType.Storage)
            .SelectMany(l => context.MaterialInventories.Where(mi =>
              mi.LocationId == l.Id &&
              mi.Quantity > 0 &&
              mi.AccountId == accountId))
            .Select(mi => mi.MaterialId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var data = await context.Materials
            .AsNoTracking()
            .Where(m => pickableMaterialIds.Contains(m.Id))
            .OrderBy(m => m.Name)
            .Select(m => new MaterialDto(m.Id, m.Name, m.Sku, m.MaterialType.ToString()))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, ShortExpiry, cancellationToken);
        return data;
    }

    public async Task<IEnumerable<AppointmentDto>> GetOutboundAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        const string key = $"{CachePrefix}outbound-appointments";
        var cached = await cache.GetAsync<IEnumerable<AppointmentDto>>(key, cancellationToken);
        if (cached != null) return cached;

        var data = await context.DockAppointments
            .Include(a => a.Truck)
            .AsNoTracking()
            .Where(a => a.Type == AppointmentType.Shipping && a.Status == AppointmentStatus.InProgress)
            .OrderBy(a => a.StartDateTime)
            .Select(a => new AppointmentDto(
              a.Id,
              a.Truck != null ? a.Truck.LicensePlate : "N/A",
              a.StartDateTime))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(key, data, ShortExpiry, cancellationToken);
        return data;
    }

    public async Task<PagedResult<RepackableInventoryDto>> GetRepackableInventoryAsync(Guid accountId, Guid? materialId, string? searchTerm, int page, int pageSize, bool includeAllLocations, CancellationToken cancellationToken = default)
    {
        // Very dynamic data frequently filtered, usually better to skip cache or use a very short one
        var query = context.MaterialInventories
            .AsNoTracking()
            .Include(mi => mi.Material)
            .Include(mi => mi.Location)
            .Include(mi => mi.Pallet)
            .Where(mi => mi.AccountId == accountId && mi.Quantity > 0);

        if (!includeAllLocations)
        {
            query = query.Where(mi => mi.Location.ZoneType == LocationType.Storage);
        }

        if (materialId.HasValue)
        {
            query = query.Where(mi => mi.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(mi =>
                mi.Pallet.Barcode.ToLower().Contains(term) ||
                mi.Material.Name.ToLower().Contains(term) ||
                mi.Material.Sku.ToLower().Contains(term) ||
                mi.Barcode.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(mi => mi.Pallet.Barcode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(mi => new RepackableInventoryDto(
              mi.Id,
              mi.MaterialId,
              mi.Material.Name,
              mi.Material.Sku,
              mi.Location.Barcode,
              mi.Quantity,
              mi.Pallet.Barcode,
              mi.BatchNumber,
              mi.Barcode
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<RepackableInventoryDto> { Items = items, TotalCount = totalCount };
    }

    public async Task<string> GetBarcodeHtmlAsync(string barcodeText, string type, decimal? quantity, CancellationToken cancellationToken = default)
    {
        string key = $"{CachePrefix}barcode:{type}:{barcodeText}:{quantity ?? 0}";
        var cached = await cache.GetAsync<string>(key, cancellationToken);
        if (cached != null) return cached;

        // --- Simulated Data Retrieval for Rich Label ---
        string materialName = "N/A";
        string palletNumber = "N/A";

        if (type == "Item")
        {
            var inventoryItem = await context.MaterialInventories
                .Include(mi => mi.Material)
                .Include(mi => mi.Pallet)
                .AsNoTracking()
                .FirstOrDefaultAsync(mi => mi.Barcode == barcodeText, cancellationToken);

            if (inventoryItem != null)
            {
                materialName = $"{inventoryItem.Material.Name} ({inventoryItem.Material.Sku})";
                palletNumber = $"Pallet #{inventoryItem.Pallet.PalletNumber}";
            }
        }
        else if (type == "Pallet")
        {
            var pallet = await context.Pallets.AsNoTracking().FirstOrDefaultAsync(p => p.Id.ToString() == barcodeText, cancellationToken);
            if (pallet != null)
            {
                materialName = "MIXED/PALLET";
                palletNumber = $"Pallet #{pallet.PalletNumber}";
                barcodeText = pallet.Barcode;
            }
        }

        string title = type == "Item" ? "ITEM LPN LABEL" : "PALLET SSCC LABEL";
        string qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={Uri.EscapeDataString(barcodeText)}";
        string companyName = (await context.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken))?.Name ?? "WMS-PHL";

        var html = $@"
            <html>
            <head>
                <style>
                    body {{ margin: 0; font-family: 'Arial', sans-serif; padding: 0; box-sizing: border-box; }}
                    .label-container {{ width: 400px; height: 160px; border: 2px solid black; display: flex; box-sizing: border-box; font-size: 10px; overflow: hidden; }}
                    .qr-section {{ flex: 0 0 140px; text-align: center; border-right: 1px solid black; padding: 5px 3px; box-sizing: border-box; display: flex; flex-direction: column; justify-content: space-between; }}
                    .text-section {{ flex: 1; padding: 5px 8px; box-sizing: border-box; display: flex; flex-direction: column; justify-content: space-between; }}
                    .header-info {{ text-align: left; }}
                    .header-info h4 {{ margin: 0 0 2px 0; font-size: 14px; font-weight: 700; line-height: 1.1; }}
                    .header-info p {{ margin: 0; font-size: 10px; line-height: 1.2; }}
                    img {{ width: 100%; height: auto; max-height: 100px; margin: 0; padding: 0; }}
                    .barcode-text {{ font-size: 14px; font-weight: bold; margin-top: 5px; line-height: 1.1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
                    .footer-details {{ border-top: 1px solid black; padding-top: 5px; }}
                    .footer-details p {{ margin: 0; font-size: 10px; line-height: 1.2; }}
                </style>
            </head>
            <body>
                <div class='label-container'>
                    <div class='qr-section'>
                        <img src='{qrCodeUrl}' alt='QR Code' />
                        <div class='barcode-text'>{barcodeText}</div>
                    </div>
                    <div class='text-section'>
                        <div class='header-info'>
                            <h4>{title}</h4>
                            <p>
                                {companyName} <br>
                                {palletNumber} <br>
                                Date: {DateTime.Now:MM/dd/yyyy}
                            </p>
                        </div>
                        <div class='footer-details'>
                            <p>
                                Contents: {materialName} <br>
                                Qty: {quantity?.ToString("N0") ?? "N/A"} <br>
                                Add. Exp: TRACEABLE (FDA/ISO)
                            </p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";

        await cache.SetAsync(key, html, DefaultExpiry, cancellationToken);
        return html;
    }

    public async Task<string> DiagnoseBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        // Diagnostics should never be cached
        var sb = new StringBuilder();
        sb.AppendLine($"Diagnosing Barcode: {barcode}");

        var palletLines = await context.Set<WMS.Domain.Entities.Transaction.PalletLine>()
            .Include(pl => pl.Pallet)
            .AsNoTracking()
            .Where(pl => pl.Barcode == barcode)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"Found {palletLines.Count} PalletLines.");
        foreach (var pl in palletLines)
        {
            sb.AppendLine($" - PalletLineId: {pl.Id}, Status: {pl.Status}, PalletId: {pl.PalletId}, PalletStatus: {pl.Pallet.Status}, AccountId: {pl.AccountId}");
        }

        var inventory = await context.MaterialInventories
            .Include(mi => mi.Location)
            .AsNoTracking()
            .Where(mi => mi.Barcode == barcode)
            .ToListAsync(cancellationToken);

        sb.AppendLine($"Found {inventory.Count} MaterialInventory records.");
        foreach (var inv in inventory)
        {
            sb.AppendLine($" - InventoryId: {inv.Id}, Qty: {inv.Quantity}, Location: {inv.Location.Barcode} (Zone: {inv.Location.ZoneType}), AccountId: {inv.AccountId}");
        }

        return sb.ToString();
    }
}
