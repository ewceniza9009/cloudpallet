using Riok.Mapperly.Abstractions;
using WMS.Application.Features.Admin.Queries;
using WMS.Application.Features.Billing.Queries;
using WMS.Application.Features.Inventory.Queries;
using WMS.Application.Features.Manifests.Queries;
using WMS.Application.Features.Shipments.Queries;
using WMS.Application.Features.Yard.Queries;
using WMS.Domain.Aggregates.Cargo;
using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Common.Mappings;

[Mapper]
public partial class WmsMapper : IWmsMapper
{
    // Admin
    [MapProperty(nameof(User.UserName), nameof(UserDto.Username))]
    public partial UserDto MapToDto(User user);
    public partial IEnumerable<UserDto> MapToDtos(IEnumerable<User> users);

    // Manifest
    [MapProperty(nameof(CargoManifest.DockAppointmentId), nameof(CargoManifestDto.AppointmentId))]
    public partial CargoManifestDto MapToDto(CargoManifest manifest);

    [MapperIgnoreTarget(nameof(CargoManifestLineDto.MaterialName))]
    [MapProperty(nameof(CargoManifestLine.Quantity), nameof(CargoManifestLineDto.ExpectedQuantity))]
    public partial CargoManifestLineDto MapToDto(CargoManifestLine line);

    public partial IEnumerable<CargoManifestLineDto> MapToDtos(IEnumerable<CargoManifestLine> lines);

    // Dock & Yard
    [MapProperty("Truck.LicensePlate", nameof(DockAppointmentDto.LicensePlate))]
    [MapperIgnoreTarget(nameof(DockAppointmentDto.YardSpotNumber))]
    public partial DockAppointmentDto MapToDto(DockAppointment appointment);

    public partial IEnumerable<DockAppointmentDto> MapToDtos(IEnumerable<DockAppointment> appointments);

    [MapProperty(nameof(DockAppointment.Id), nameof(YardAppointmentDto.AppointmentId))]
    [MapProperty(nameof(DockAppointment.StartDateTime), nameof(YardAppointmentDto.AppointmentTime))]
    [MapProperty("Truck.LicensePlate", nameof(YardAppointmentDto.LicensePlate))]
    [MapProperty("Dock.Name", nameof(YardAppointmentDto.DockName))]
    [MapProperty("Truck.Carrier.Name", nameof(YardAppointmentDto.CarrierName))]
    public partial YardAppointmentDto MapToYardDto(DockAppointment appointment);

    public partial IEnumerable<YardAppointmentDto> MapToYardDtos(IEnumerable<DockAppointment> appointments);

    // Receiving
    [MapProperty(nameof(Receiving.Id), nameof(ReceivingSessionDetailDto.ReceivingId))]
    [MapProperty(nameof(Receiving.AppointmentId), nameof(ReceivingSessionDetailDto.DockAppointmentId))]
    public partial ReceivingSessionDetailDto MapToDto(Receiving receiving);

    [MapperIgnoreTarget(nameof(PalletDetailDto.PalletTypeName))]
    public partial PalletDetailDto MapToDto(Pallet pallet);

    [MapperIgnoreTarget(nameof(PalletLineDetailDto.MaterialName))]
    [MapProperty(nameof(PalletLine.Id), nameof(PalletLineDetailDto.PalletLineId))]
    [MapProperty(nameof(PalletLine.Weight), nameof(PalletLineDetailDto.NetWeight))]
    public partial PalletLineDetailDto MapToDto(PalletLine line);

    public partial IEnumerable<PalletLineDetailDto> MapToDtos(IEnumerable<PalletLine> lines);

    // Picking - Mapperly handles records/constructors automatically by name
    [MapProperty(nameof(PickTransaction.Id), "PickId")]
    [MapProperty("MaterialInventory.Material.Name", "Material")]
    [MapProperty("MaterialInventory.Material.Sku", "Sku")]
    [MapProperty("MaterialInventory.Location.Barcode", "Location")]
    public partial PickItemDto MapToDto(PickTransaction transaction);

    public partial IEnumerable<PickItemDto> MapToDtos(IEnumerable<PickTransaction> transactions);

    // Billing
    public partial InvoiceDto MapToDto(Invoice invoice);

    // Custom mapping for InvoiceDetailDto due to deep nesting (Account.Name)
    [MapProperty("Account.Name", nameof(InvoiceDetailDto.Name))]
    public partial InvoiceDetailDto MapToDetailDto(Invoice invoice);

    public partial InvoiceLineDto MapToDto(InvoiceLine line);

    // --- Custom Mappings ---

    private string MapEnumToString<T>(T value) where T : struct, System.Enum => value.ToString();

    private string MapLicensePlate(string? value) => value ?? "N/A";
}
