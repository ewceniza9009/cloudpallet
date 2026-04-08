using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Abstractions.Services;
using WMS.Application.Common.Models;
using WMS.Application.Features.Lookups;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LookupsController(ILookupService lookupService) : ControllerBase
{
    [HttpGet("warehouses")]
    [ProducesResponseType(typeof(IEnumerable<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouses(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetWarehousesAsync(cancellationToken));
    }

    [HttpGet("pallet-types")]
    [ProducesResponseType(typeof(IEnumerable<PalletTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPalletTypes(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetPalletTypesAsync(cancellationToken));
    }

    [HttpGet("docks")]
    [ProducesResponseType(typeof(IEnumerable<DockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocks([FromQuery] Guid warehouseId, CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetDocksAsync(warehouseId, cancellationToken));
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetSuppliersAsync(cancellationToken));
    }

    [HttpGet("materials")]
    public async Task<IActionResult> GetMaterials(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetMaterialsAsync(cancellationToken));
    }

    [HttpGet("material-categories")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaterialCategories(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetMaterialCategoriesAsync(cancellationToken));
    }

    [HttpGet("uoms")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUoms(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetUomsAsync(cancellationToken));
    }

    [HttpGet("materials/search")]
    [ProducesResponseType(typeof(PagedResult<MaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMaterials(
      [FromQuery] string? searchTerm,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 20,
      CancellationToken cancellationToken = default)
    {
        return Ok(await lookupService.SearchMaterialsAsync(searchTerm, page, pageSize, cancellationToken));
    }

    [HttpGet("active-appointments")]
    public async Task<IActionResult> GetActiveAppointments(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetActiveAppointmentsAsync(cancellationToken));
    }

    [HttpGet("available-yard-spots")]
    [ProducesResponseType(typeof(IEnumerable<YardSpotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableYardSpots([FromQuery] Guid warehouseId, CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetAvailableYardSpotsAsync(warehouseId, cancellationToken));
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetAccountsAsync(cancellationToken));
    }

    [HttpGet("trucks")]
    [ProducesResponseType(typeof(IEnumerable<TruckDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrucks(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetTrucksAsync(cancellationToken));
    }

    [HttpGet("available-storage-locations")]
    [ProducesResponseType(typeof(IEnumerable<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableStorageLocations([FromQuery] Guid warehouseId, CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetAvailableStorageLocationsAsync(warehouseId, cancellationToken));
    }

    [HttpGet("pickable-materials")]
    [ProducesResponseType(typeof(IEnumerable<MaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPickableMaterials([FromQuery] Guid warehouseId, [FromQuery] Guid accountId, CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetPickableMaterialsAsync(warehouseId, accountId, cancellationToken));
    }

    [HttpGet("outbound-appointments")]
    public async Task<IActionResult> GetOutboundAppointments(CancellationToken cancellationToken)
    {
        return Ok(await lookupService.GetOutboundAppointmentsAsync(cancellationToken));
    }

    [HttpGet("repackable-inventory")]
    [ProducesResponseType(typeof(PagedResult<RepackableInventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepackableInventory(
        [FromQuery] Guid accountId,
        [FromQuery] Guid? materialId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeAllLocations = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await lookupService.GetRepackableInventoryAsync(accountId, materialId, searchTerm, page, pageSize, includeAllLocations, cancellationToken));
    }

    [HttpGet("barcode-image")]
    [ProducesResponseType(typeof(ContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBarcodeImage(
        [FromQuery] string barcodeText, 
        [FromQuery] string type, 
        [FromQuery] decimal? quantity,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcodeText))
        {
            return BadRequest("Barcode text is required.");
        }

        var htmlContent = await lookupService.GetBarcodeHtmlAsync(barcodeText, type, quantity, cancellationToken);
        return Content(htmlContent, "text/html", System.Text.Encoding.UTF8);
    }

    [HttpGet("diagnose-barcode")]
    public async Task<IActionResult> DiagnoseBarcode([FromQuery] string barcode, CancellationToken cancellationToken)
    {
        return Ok(await lookupService.DiagnoseBarcodeAsync(barcode, cancellationToken));
    }
}