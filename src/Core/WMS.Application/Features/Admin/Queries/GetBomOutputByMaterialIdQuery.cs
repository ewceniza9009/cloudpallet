using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Admin.Commands;
using WMS.Domain.Entities;

namespace WMS.Application.Features.Admin.Queries;

public record BomDto(
    Guid Id,
    Guid OutputMaterialId,
    decimal OutputQuantity,
    List<BomLineDto> Lines);

public record GetBomByOutputMaterialIdQuery(Guid OutputMaterialId) : IRequest<BomDto?>;

public class GetBomByOutputMaterialIdQueryHandler(
    IBillOfMaterialRepository bomRepository) : IRequestHandler<GetBomByOutputMaterialIdQuery, BomDto?>
{
    public async Task<BomDto?> Handle(GetBomByOutputMaterialIdQuery request, CancellationToken cancellationToken)
    {
        var bom = await bomRepository.GetByOutputMaterialIdAsync(request.OutputMaterialId, cancellationToken);

        if (bom is null)
        {
            return null;
        }

        return new BomDto(
            bom.Id,
            bom.OutputMaterialId,
            bom.OutputQuantity,
            bom.Lines.Select(l => new BomLineDto(l.InputMaterialId, l.InputQuantity)).ToList()
        );
    }
}