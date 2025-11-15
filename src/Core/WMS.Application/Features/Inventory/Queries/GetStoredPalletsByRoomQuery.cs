using MediatR;
using WMS.Application.Abstractions.Persistence;
using System.Text;

namespace WMS.Application.Features.Inventory.Queries;

public record PalletLineItemDto
{
    public Guid InventoryId { get; set; }
    public string MaterialName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Barcode { get; init; } = string.Empty;       
}

public record StoredPalletDetailDto
{    
    public Guid PalletId { get; init; }
    public string PalletBarcode { get; init; } = string.Empty;
    public Guid CurrentLocationId { get; init; }
    public string CurrentLocationBarcode { get; init; } = string.Empty;
    public List<PalletLineItemDto> Lines { get; init; } = new();
}

public record RoomWithPalletsDto
{
    public string RoomName { get; init; } = string.Empty;
    public List<StoredPalletDetailDto> Pallets { get; init; } = new();
}

public record GetStoredPalletsByRoomQuery(Guid WarehouseId) : IRequest<IEnumerable<RoomWithPalletsDto>>;

public class GetStoredPalletsByRoomQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetStoredPalletsByRoomQuery, IEnumerable<RoomWithPalletsDto>>
{
    public async Task<IEnumerable<RoomWithPalletsDto>> Handle(GetStoredPalletsByRoomQuery request, CancellationToken cancellationToken)
    {
        return await warehouseRepository.GetStoredPalletsByRoomAsync(request.WarehouseId, cancellationToken);
    }
}