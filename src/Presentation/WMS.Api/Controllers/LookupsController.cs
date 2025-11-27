using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WMS.Application.Abstractions.Integrations;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;
using WMS.Application.Features.Warehouse.Queries;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Persistence.Repositories;

namespace WMS.Api.Controllers;

public record SupplierDto(Guid Id, string Name);
public record MaterialDto(Guid Id, string Name, string Sku, string MaterialType);
public record AppointmentDto(Guid Id, string LicensePlate, DateTime StartTime);
public record AccountDto(Guid Id, string Name);
public record DockDto(
  Guid Id,
  string Name,
  bool IsAvailable,
  Guid? CurrentAppointmentId,
  string? LicensePlate,
  string? CarrierName,
  DateTime? Arrival);
public record WarehouseDto(Guid Id, string Name);
public record LocationDto(Guid Id, string DisplayName);
public record YardSpotDto(Guid Id, string SpotNumber);
public record TruckDto(Guid Id, string LicensePlate);
public record LookupDto(Guid Id, string Name);
public record PalletTypeDto(Guid Id, string Name, decimal TareWeight);
public record RepackableInventoryDto(Guid InventoryId, Guid MaterialId, string MaterialName, string Sku, string Location, decimal Quantity, string PalletBarcode, string? BatchNumber);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LookupsController(WmsDbContext context) : ControllerBase
{
    [HttpGet("warehouses")]
    [ProducesResponseType(typeof(IEnumerable<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouses()
    {
        return Ok(await context.Warehouses
          .AsNoTracking()
          .OrderBy(w => w.Name)
          .Select(w => new WarehouseDto(w.Id, w.Name))
          .ToListAsync());
    }

    [HttpGet("pallet-types")]
    [ProducesResponseType(typeof(IEnumerable<PalletTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPalletTypes()
    {
        return Ok(await context.PalletTypes
          .AsNoTracking()
          .Where(pt => pt.IsActive)
          .OrderBy(pt => pt.Name)
          .Select(pt => new PalletTypeDto(pt.Id, pt.Name, pt.TareWeight))
          .ToListAsync());
    }

    [HttpGet("docks")]
    [ProducesResponseType(typeof(IEnumerable<DockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocks([FromQuery] Guid warehouseId)
    {
        var docksData = await (from dock in context.Docks.AsNoTracking().Where(d => d.WarehouseId == warehouseId)
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
                               )).ToListAsync();

        return Ok(docksData);
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers()
    {
        return Ok(await context.Suppliers
          .AsNoTracking()
          .OrderBy(s => s.Name)
          .Select(s => new SupplierDto(s.Id, s.Name))
          .ToListAsync());
    }

    [HttpGet("materials")]
    public async Task<IActionResult> GetMaterials()
    {
        return Ok(await context.Materials
          .AsNoTracking()
          .OrderBy(m => m.Name)
          .Select(m => new MaterialDto(m.Id, m.Name, m.Sku, m.MaterialType.ToString()))
          .ToListAsync());
    }

    [HttpGet("material-categories")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaterialCategories()
    {
        return Ok(await context.MaterialCategories
          .AsNoTracking()
          .OrderBy(c => c.Name)
          .Select(c => new LookupDto(c.Id, c.Name))
          .ToListAsync());
    }

    [HttpGet("uoms")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUoms()
    {
        return Ok(await context.UnitsOfMeasure
          .AsNoTracking()
          .OrderBy(u => u.Name)
          .Select(u => new LookupDto(u.Id, u.Name))
          .ToListAsync());
    }

    [HttpGet("materials/search")]
    [ProducesResponseType(typeof(PagedResult<MaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMaterials(
      [FromQuery] string? searchTerm,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 20)
    {
        var query = context.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(m => m.Name.Contains(searchTerm) || m.Sku.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var items = await query
          .OrderBy(m => m.Name)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .Select(m => new MaterialDto(m.Id, m.Name, m.Sku, m.MaterialType.ToString()))
          .ToListAsync();

        return Ok(new PagedResult<MaterialDto> { Items = items, TotalCount = totalCount });
    }

    [HttpGet("active-appointments")]
    public async Task<IActionResult> GetActiveAppointments()
    {
        var relevantAppointments = await context.DockAppointments
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
          .ToListAsync();

        return Ok(relevantAppointments);
    }

    [HttpGet("available-yard-spots")]
    [ProducesResponseType(typeof(IEnumerable<YardSpotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableYardSpots([FromQuery] Guid warehouseId)
    {
        var spots = await context.YardSpots
          .AsNoTracking()
          .Where(ys => ys.WarehouseId == warehouseId && ys.Status == YardSpotStatus.Available)
          .OrderBy(ys => ys.SpotNumber)
          .Select(ys => new YardSpotDto(ys.Id, ys.SpotNumber))
          .ToListAsync();

        return Ok(spots);
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        return Ok(await context.Accounts
          .AsNoTracking()
          .OrderBy(a => a.Name)
          .Select(a => new AccountDto(a.Id, a.Name))
          .ToListAsync());
    }

    [HttpGet("trucks")]
    [ProducesResponseType(typeof(IEnumerable<TruckDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrucks()
    {
        return Ok(await context.Trucks
          .AsNoTracking()
          .OrderBy(t => t.LicensePlate)
          .Select(t => new TruckDto(t.Id, t.LicensePlate))
          .ToListAsync());
    }

    [HttpGet("available-storage-locations")]
    [ProducesResponseType(typeof(IEnumerable<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableStorageLocations([FromQuery] Guid warehouseId)
    {
        var locations = await context.Warehouses
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
          .ToListAsync();

        return Ok(locations);
    }

    [HttpGet("pickable-materials")]
    [ProducesResponseType(typeof(IEnumerable<MaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPickableMaterials([FromQuery] Guid warehouseId, [FromQuery] Guid accountId)
    {
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
          .ToListAsync();

        var materials = await context.Materials
          .AsNoTracking()
          .Where(m => pickableMaterialIds.Contains(m.Id))
          .OrderBy(m => m.Name)
          .Select(m => new MaterialDto(m.Id, m.Name, m.Sku, m.MaterialType.ToString()))
          .ToListAsync();

        return Ok(materials);
    }

    [HttpGet("outbound-appointments")]
    public async Task<IActionResult> GetOutboundAppointments()
    {
        var relevantAppointments = await context.DockAppointments
          .Include(a => a.Truck)
          .AsNoTracking()
          .Where(a => a.Type == AppointmentType.Shipping && a.Status == AppointmentStatus.InProgress)
          .OrderBy(a => a.StartDateTime)
          .Select(a => new AppointmentDto(
            a.Id,
            a.Truck != null ? a.Truck.LicensePlate : "N/A",
            a.StartDateTime))
          .ToListAsync();

        return Ok(relevantAppointments);
    }

    [HttpGet("repackable-inventory")]
    [ProducesResponseType(typeof(PagedResult<RepackableInventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepackableInventory(
        [FromQuery] Guid accountId,
        [FromQuery] Guid? materialId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = context.MaterialInventories
          .AsNoTracking()
          .Include(mi => mi.Material)
          .Include(mi => mi.Location)
          .Include(mi => mi.Pallet)
          .Where(mi => mi.AccountId == accountId && mi.Quantity > 0 && mi.Location.ZoneType == LocationType.Storage);

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
                mi.Material.Sku.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

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
            mi.BatchNumber
          ))
          .ToListAsync();

        return Ok(new PagedResult<RepackableInventoryDto> { Items = items, TotalCount = totalCount });
    }

    [HttpGet("barcode-image")]
    [ProducesResponseType(typeof(ContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetBarcodeImage([FromQuery] string barcodeText, [FromQuery] string type, [FromQuery] decimal? quantity)
    {
        if (string.IsNullOrWhiteSpace(barcodeText))
        {
            return BadRequest("Barcode text is required.");
        }

        // --- Simulated Data Retrieval for Rich Label ---
        string materialName = "N/A";
        string palletNumber = "N/A";

        if (type == "Item")
        {
            var inventoryItem = context.MaterialInventories
                .Include(mi => mi.Material)
                .Include(mi => mi.Pallet)
                .FirstOrDefault(mi => mi.Barcode == barcodeText);

            if (inventoryItem != null)
            {
                materialName = $"{inventoryItem.Material.Name} ({inventoryItem.Material.Sku})";
                palletNumber = $"Pallet #{inventoryItem.Pallet.PalletNumber}";
            }
        }
        else if (type == "Pallet")
        {
            var pallet = context.Pallets.FirstOrDefault(p => p.Id.ToString() == barcodeText);
            if (pallet != null)
            {
                materialName = "MIXED/PALLET";
                palletNumber = $"Pallet #{pallet.PalletNumber}";

                barcodeText = pallet.Barcode;
            }
        }

        string title = type == "Item" ? "ITEM LPN LABEL" : "PALLET SSCC LABEL";
        string identifier = type == "Item" ? "LPN/SSCC" : "PALLET SSCC";

        // Use a public API for QR code generation for simulation purposes 
        string qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={Uri.EscapeDataString(barcodeText)}";

        // Construct the HTML/SVG content for printing - OPTIMIZED FOR SPACE AND LOOK
        var htmlContent = $@"
            <html>
            <head>
                <style>
                    /* Reset margins and ensure content fits tightly */
                    body {{ margin: 0; font-family: 'Arial', sans-serif; padding: 0; box-sizing: border-box; }}
                    
                    /* Aggressive sizing to maximize space */
                    .label-container {{ 
                        width: 400px; /* Increased overall width */
                        height: 160px; 
                        border: 2px solid black; 
                        display: flex; 
                        box-sizing: border-box; 
                        font-size: 10px; 
                        overflow: hidden; /* Ensure nothing spills out */
                    }}
                    
                    /* QR section fixed width, tight padding */
                    .qr-section {{ 
                        flex: 0 0 140px; /* Fixed width for QR */
                        text-align: center; 
                        border-right: 1px solid black; 
                        padding: 5px 3px; /* Adjusted padding */
                        box-sizing: border-box; 
                        display: flex; 
                        flex-direction: column; 
                        justify-content: space-between;
                    }}
                    
                    /* Text section fills remaining space, very tight padding */
                    .text-section {{ 
                        flex: 1; 
                        padding: 5px 8px; /* Slightly more horizontal padding */
                        box-sizing: border-box; 
                        display: flex; 
                        flex-direction: column; 
                        justify-content: space-between; 
                    }}
                    
                    .header-info {{ text-align: left; }}
                    /* Increased font size for key details */
                    .header-info h4 {{ margin: 0 0 2px 0; font-size: 14px; font-weight: 700; line-height: 1.1; }}
                    .header-info p {{ margin: 0; font-size: 10px; line-height: 1.2; }}
                    
                    /* Ensure QR code fills space */
                    img {{ width: 100%; height: auto; max-height: 100px; margin: 0; padding: 0; }}
                    
                    /* Larger, bold text for the scannable number, preventing wrap */
                    .barcode-text {{ 
                        font-size: 14px; 
                        font-weight: bold; 
                        margin-top: 5px; 
                        line-height: 1.1;
                        white-space: nowrap;
                        overflow: hidden;
                        text-overflow: ellipsis;
                    }}
                    
                    /* Footer details are compact */
                    .footer-details {{ border-top: 1px solid black; padding-top: 5px; }}
                    .footer-details p {{ margin: 0; font-size: 10px; line-height: 1.2; }}
                    
                    /* Ensure flex items stretch vertically */
                    .text-section > div {{ flex-grow: 0; }}
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
                                {(context.Companies.FirstOrDefault()?.Name ?? "WMS-PHL")} <br>
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
            </html>
        ";

        return Content(htmlContent, "text/html", Encoding.UTF8);
    }
}