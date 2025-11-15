// ---- File: src/Core/WMS.Application/Common/Mappings/WmsMappingProfile.cs [FIXED] ----

using AutoMapper;
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

public class WmsMappingProfile : Profile
{
    public WmsMappingProfile()
    {
        // Admin Mappings
        CreateMap<User, UserDto>();

        // Manifest Mappings
        CreateMap<CargoManifest, CargoManifestDto>();
        CreateMap<CargoManifestLine, CargoManifestLineDto>()
            .ForMember(dest => dest.MaterialName, opt => opt.Ignore());

        // Dock & Yard Mappings
        CreateMap<DockAppointment, DockAppointmentDto>()
            .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.Truck != null ? src.Truck.LicensePlate : "N/A"));

        CreateMap<DockAppointment, YardAppointmentDto>()
           .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.Id))
           // THIS IS THE FIX: Map the full DateTime object directly.
           .ForMember(dest => dest.AppointmentTime, opt => opt.MapFrom(src => src.StartDateTime))
           .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.Truck != null ? src.Truck.LicensePlate : "N/A"))
           .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
           .ForMember(dest => dest.DockName, opt => opt.MapFrom(src => src.Dock.Name))
           .ForMember(dest => dest.CarrierName, opt => opt.MapFrom(src => src.Truck != null && src.Truck.Carrier != null ? src.Truck.Carrier.Name : "N/A"));


        // Receiving Mappings
        CreateMap<Receiving, ReceivingSessionDetailDto>()
            .ForMember(dest => dest.ReceivingId, opt => opt.MapFrom(src => src.Id));
        CreateMap<Pallet, PalletDetailDto>()
            .ForMember(dest => dest.PalletTypeName, opt => opt.MapFrom(src => src.PalletType.Name));
        CreateMap<PalletLine, PalletLineDetailDto>()
            .ForMember(dest => dest.PalletLineId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.NetWeight, opt => opt.MapFrom(src => src.Weight))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.BatchNumber, opt => opt.MapFrom(src => src.BatchNumber))
            .ForMember(dest => dest.DateOfManufacture, opt => opt.MapFrom(src => src.DateOfManufacture))
            .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.ExpiryDate));

        // Picking Mappings
        CreateMap<PickTransaction, PickItemDto>()
             .ForCtorParam("PickId", opt => opt.MapFrom(src => src.Id))
             .ForCtorParam("Material", opt => opt.MapFrom(src => src.MaterialInventory.Material.Name))
             .ForCtorParam("Sku", opt => opt.MapFrom(src => src.MaterialInventory.Material.Sku))
             .ForCtorParam("Location", opt => opt.MapFrom(src => src.MaterialInventory.Location.Barcode))
             .ForCtorParam("Quantity", opt => opt.MapFrom(src => src.Quantity))
             .ForCtorParam("Status", opt => opt.MapFrom(src => src.Status.ToString()));


        // Billing Mappings
        CreateMap<Invoice, InvoiceDto>();
        CreateMap<Invoice, InvoiceDetailDto>();
        CreateMap<InvoiceLine, InvoiceLineDto>();
    }
}