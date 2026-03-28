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

public interface IWmsMapper
{
    // Admin
    UserDto MapToDto(User user);
    IEnumerable<UserDto> MapToDtos(IEnumerable<User> users);

    // Manifest
    CargoManifestDto MapToDto(CargoManifest manifest);
    CargoManifestLineDto MapToDto(CargoManifestLine line);
    IEnumerable<CargoManifestLineDto> MapToDtos(IEnumerable<CargoManifestLine> lines);

    // Dock & Yard
    DockAppointmentDto MapToDto(DockAppointment appointment);
    IEnumerable<DockAppointmentDto> MapToDtos(IEnumerable<DockAppointment> appointments);
    YardAppointmentDto MapToYardDto(DockAppointment appointment);
    IEnumerable<YardAppointmentDto> MapToYardDtos(IEnumerable<DockAppointment> appointments);

    // Receiving
    ReceivingSessionDetailDto MapToDto(Receiving receiving);
    PalletDetailDto MapToDto(Pallet pallet);
    PalletLineDetailDto MapToDto(PalletLine line);
    IEnumerable<PalletLineDetailDto> MapToDtos(IEnumerable<PalletLine> lines);

    // Picking
    PickItemDto MapToDto(PickTransaction transaction);
    IEnumerable<PickItemDto> MapToDtos(IEnumerable<PickTransaction> transactions);

    // Billing
    InvoiceDto MapToDto(Invoice invoice);
    InvoiceDetailDto MapToDetailDto(Invoice invoice);
    InvoiceLineDto MapToDto(InvoiceLine line);
}
