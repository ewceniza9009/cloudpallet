using WMS.Application.Common.Models;

namespace WMS.Application.Features.Lookups;

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
public record RepackableInventoryDto(
    Guid InventoryId, 
    Guid MaterialId, 
    string MaterialName, 
    string Sku, 
    string Location, 
    decimal Quantity, 
    string PalletBarcode, 
    string? BatchNumber, 
    string Barcode);
