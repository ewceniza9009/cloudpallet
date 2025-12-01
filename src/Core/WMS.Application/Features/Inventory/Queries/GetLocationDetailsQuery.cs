using MediatR;
using WMS.Application.Features.Inventory.Queries;

namespace WMS.Application.Features.Inventory.Queries;

public record LocationDetailMaterialDto(
    string MaterialName,
    string Sku,
    decimal Quantity,
    string BatchNumber,
    DateTime? ExpiryDate
);

public record LocationDetailPalletDto(
    Guid PalletId,
    string Barcode,
    string Type,
    decimal Weight,
    List<LocationDetailMaterialDto> Materials
);

public record LocationDetailsDto(
    Guid LocationId,
    string LocationBarcode,
    string ZoneType,
    decimal Utilization,
    string Status,
    LocationDetailPalletDto? Pallet
);

public record GetLocationDetailsQuery(Guid LocationId) : IRequest<LocationDetailsDto?>;
