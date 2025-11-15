using MediatR;
using WMS.Application.Abstractions.Persistence;
using System.Text;

namespace WMS.Application.Features.Inventory.Queries;

public record StoredPalletDto
{
    public Guid PalletId { get; init; }
    public string PalletBarcode { get; init; } = string.Empty;
    public string Contents { get; init; } = string.Empty;
    public Guid CurrentLocationId { get; init; }
    public string CurrentLocationBarcode { get; init; } = string.Empty;
}

public record GetStoredPalletsQuery(Guid WarehouseId) : IRequest<IEnumerable<StoredPalletDto>>;

public class GetStoredPalletsQueryHandler(
    IWarehouseRepository warehouseRepository,
    IMaterialRepository materialRepository)
    : IRequestHandler<GetStoredPalletsQuery, IEnumerable<StoredPalletDto>>
{
    public async Task<IEnumerable<StoredPalletDto>> Handle(GetStoredPalletsQuery request, CancellationToken cancellationToken)
    {
        var storedPallets = await warehouseRepository.GetStoredPalletsAsync(request.WarehouseId, cancellationToken);
        var dtos = new List<StoredPalletDto>();

        foreach (var pallet in storedPallets)
        {
            var materialIds = pallet.Lines.Select(l => l.MaterialId).Distinct().ToList();
            var materials = await materialRepository.GetByIdsAsync(materialIds, cancellationToken);
            var materialMap = materials.ToDictionary(m => m.Id, m => m.Name);

            var contentsBuilder = new StringBuilder();
            if (pallet.Lines.Count > 1)
            {
                var firstMaterialName = materialMap.GetValueOrDefault(pallet.Lines.First().MaterialId);
                contentsBuilder.Append($"Mixed: {firstMaterialName} + {pallet.Lines.Count - 1} other(s)");
            }
            else if (materialMap.TryGetValue(pallet.Lines.First().MaterialId, out var materialName))
            {
                contentsBuilder.Append(materialName);
            }

            // THIS IS THE UPDATED LOGIC
            string currentLocationString = "N/A";
            var inventoryItem = pallet.Inventory.FirstOrDefault();
            if (inventoryItem?.Location != null)
            {
                var room = await warehouseRepository.GetRoomByLocationIdAsync(inventoryItem.LocationId, cancellationToken);
                var location = inventoryItem.Location;
                //currentLocationString = $"{room?.Name} / {location.Bay} / R{location.Row}C{location.Column}L{location.Level} / {location.Barcode}";
                currentLocationString = $"{room?.Name} / {location.Bay} / {location.Barcode}";
            }

            dtos.Add(new StoredPalletDto
            {
                PalletId = pallet.Id,
                PalletBarcode = pallet.Barcode,
                Contents = contentsBuilder.ToString(),
                CurrentLocationId = inventoryItem?.LocationId ?? Guid.Empty,
                CurrentLocationBarcode = currentLocationString
            });
        }

        return dtos;
    }
}