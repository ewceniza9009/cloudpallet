using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Queries;

public class ReceivingVarianceLineDto
{
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal Variance => ReceivedQuantity - ExpectedQuantity;
    public string Status { get; set; } = "Unknown"; // Match, Shortage, Overage
}

public class ReceivingVarianceDto
{
    public Guid ReceivingId { get; set; }
    public Guid? ManifestId { get; set; }
    public List<ReceivingVarianceLineDto> Lines { get; set; } = new();
}

public record GetReceivingVarianceQuery(Guid ReceivingId) : IRequest<ReceivingVarianceDto?>;

public class GetReceivingVarianceQueryHandler(
    IReceivingTransactionRepository receivingRepository,
    ICargoManifestRepository manifestRepository,
    IMaterialRepository materialRepository) : IRequestHandler<GetReceivingVarianceQuery, ReceivingVarianceDto?>
{
    public async Task<ReceivingVarianceDto?> Handle(GetReceivingVarianceQuery request, CancellationToken cancellationToken)
    {
        var receiving = await receivingRepository.GetByIdWithDetailsAsync(request.ReceivingId, cancellationToken);
        if (receiving is null) return null;

        var manifest = await manifestRepository.GetByAppointmentIdAsync(receiving.AppointmentId, cancellationToken);
        
        // If no manifest, we can't calculate variance in the traditional sense, 
        // but we could return what was received vs 0 expected.
        // For now, let's assume if no manifest, we return empty or specific indication.
        
        var response = new ReceivingVarianceDto
        {
            ReceivingId = receiving.Id,
            ManifestId = manifest?.Id
        };

        var receivedMap = receiving.Pallets
            .SelectMany(p => p.Lines)
            .GroupBy(l => l.MaterialId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var expectedMap = manifest?.Lines
            .GroupBy(l => l.MaterialId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity)) ?? new Dictionary<Guid, decimal>();

        var allMaterialIds = receivedMap.Keys.Union(expectedMap.Keys).Distinct().ToList();
        var materials = await materialRepository.GetByIdsAsync(allMaterialIds, cancellationToken);
        var materialNameMap = materials.ToDictionary(m => m.Id, m => m.Name);

        foreach (var materialId in allMaterialIds)
        {
            var expected = expectedMap.TryGetValue(materialId, out var exp) ? exp : 0;
            var received = receivedMap.TryGetValue(materialId, out var rec) ? rec : 0;
            var name = materialNameMap.TryGetValue(materialId, out var n) ? n : "Unknown Material";

            string status;
            if (received == expected) status = "Match";
            else if (received < expected) status = "Shortage";
            else status = "Overage";

            response.Lines.Add(new ReceivingVarianceLineDto
            {
                MaterialId = materialId,
                MaterialName = name,
                ExpectedQuantity = expected,
                ReceivedQuantity = received,
                Status = status
            });
        }

        return response;
    }
}
