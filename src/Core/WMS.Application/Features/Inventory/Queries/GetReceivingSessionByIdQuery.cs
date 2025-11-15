using AutoMapper;
using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Features.Inventory.Queries;

public class PalletLineDetailDto
{
    public Guid PalletLineId { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal NetWeight { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime DateOfManufacture { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class PalletDetailDto
{
    public Guid Id { get; set; }
    public string PalletNumber { get; set; } = string.Empty;
    public string PalletTypeName { get; set; } = string.Empty;    
    public decimal TareWeight { get; set; }
    public List<PalletLineDetailDto> Lines { get; set; } = new();
}

public class ReceivingSessionDetailDto
{
    public Guid ReceivingId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? DockAppointmentId { get; set; }
    public List<PalletDetailDto> Pallets { get; set; } = new();
}


public record GetReceivingSessionByIdQuery(Guid ReceivingId) : IRequest<ReceivingSessionDetailDto?>;

public class GetReceivingSessionByIdQueryHandler(
    IReceivingTransactionRepository receivingRepository,
    IMaterialRepository materialRepository,
    IMapper mapper)
    : IRequestHandler<GetReceivingSessionByIdQuery, ReceivingSessionDetailDto?>
{
    public async Task<ReceivingSessionDetailDto?> Handle(GetReceivingSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var receiving = await receivingRepository.GetByIdWithDetailsAsync(request.ReceivingId, cancellationToken);
        if (receiving is null) return null;

        var dto = mapper.Map<ReceivingSessionDetailDto>(receiving);

        var materialIds = dto.Pallets.SelectMany(p => p.Lines.Select(l => l.MaterialId)).Distinct().ToList();
        var materials = await materialRepository.GetByIdsAsync(materialIds, cancellationToken);
        var materialMap = materials.ToDictionary(m => m.Id, m => m.Name);

        foreach (var pallet in dto.Pallets)
        {
            foreach (var line in pallet.Lines)
            {
                if (materialMap.TryGetValue(line.MaterialId, out var name))
                {
                    line.MaterialName = name;
                }
            }
        }

        return dto;
    }
}