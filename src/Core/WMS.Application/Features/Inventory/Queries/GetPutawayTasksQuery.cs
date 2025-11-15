using MediatR;
using System.Text;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Services;

namespace WMS.Application.Features.Inventory.Queries;

public record PutawayTaskDto
{
    public Guid PalletId { get; init; }
    public string PalletBarcode { get; init; } = string.Empty;
    public string Contents { get; init; } = string.Empty;
    public string CurrentLocation { get; init; } = string.Empty;
    public Guid SuggestedLocationId { get; init; }
    public string SuggestedLocation { get; init; } = string.Empty;
}

public record GetPutawayTasksQuery(Guid WarehouseId) : IRequest<IEnumerable<PutawayTaskDto>>;

public class GetPutawayTasksQueryHandler(
    IWarehouseRepository warehouseRepository,
    ILocationRepository locationRepository,
    IMaterialRepository materialRepository,
    IPutawaySuggestionService suggestionService)
    : IRequestHandler<GetPutawayTasksQuery, IEnumerable<PutawayTaskDto>>
{
    public async Task<IEnumerable<PutawayTaskDto>> Handle(GetPutawayTasksQuery request, CancellationToken cancellationToken)
    {
        var putawayTasks = new List<PutawayTaskDto>();
        var palletsInStaging = await warehouseRepository.GetPalletsInStagingAsync(request.WarehouseId, cancellationToken);

        foreach (var pallet in palletsInStaging)
        {
            var firstLine = pallet.Lines.FirstOrDefault();
            if (firstLine is null) continue;

            var suggestedLocationId = await suggestionService.SuggestLocationAsync(firstLine.MaterialId, cancellationToken);
            var suggestedLocation = await locationRepository.GetByIdAsync(suggestedLocationId, cancellationToken);

            string suggestedLocationString = "N/A";

            if (suggestedLocation != null)
            {
                var room = await warehouseRepository.GetRoomByLocationIdAsync(suggestedLocation.Id, cancellationToken);
                //suggestedLocationString = $"{room?.Name} / {suggestedLocation.Bay} / R{suggestedLocation.Row}C{suggestedLocation.Column}L{suggestedLocation.Level} / {suggestedLocation.Barcode}";
                suggestedLocationString = $"{room?.Name} / {suggestedLocation.Bay} / {suggestedLocation.Barcode}";
            }

            var materialIds = pallet.Lines.Select(l => l.MaterialId).Distinct().ToList();
            var materials = await materialRepository.GetByIdsAsync(materialIds, cancellationToken);
            var materialMap = materials.ToDictionary(m => m.Id, m => m.Name);

            var contentsBuilder = new StringBuilder();
            if (pallet.Lines.Count > 1)
            {
                var firstMaterialName = materialMap.GetValueOrDefault(pallet.Lines.First().MaterialId);
                contentsBuilder.Append($"Mixed: {firstMaterialName} + {pallet.Lines.Count - 1} other(s)");
            }
            else if (materialMap.TryGetValue(firstLine.MaterialId, out var materialName))
            {
                contentsBuilder.Append(materialName);
            }

            var task = new PutawayTaskDto
            {
                PalletId = pallet.Id,
                PalletBarcode = pallet.Barcode,
                Contents = contentsBuilder.ToString(),
                CurrentLocation = "Receiving Staging",
                SuggestedLocationId = suggestedLocationId,
                SuggestedLocation = suggestedLocationString      
            };

            putawayTasks.Add(task);
        }

        return putawayTasks;
    }
}