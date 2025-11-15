using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record UnitOfMeasureDetailDto(
    Guid Id,
    string Name,
    string Symbol);

public record GetUnitOfMeasureByIdQuery(Guid Id) : IRequest<UnitOfMeasureDetailDto?>;

public class GetUnitOfMeasureByIdQueryHandler(IUnitOfMeasureRepository uomRepository)
    : IRequestHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDetailDto?>
{
    public async Task<UnitOfMeasureDetailDto?> Handle(GetUnitOfMeasureByIdQuery request, CancellationToken cancellationToken)
    {
        var uom = await uomRepository.GetByIdAsync(request.Id, cancellationToken);
        if (uom == null) return null;

        return new UnitOfMeasureDetailDto(
            uom.Id,
            uom.Name,
            uom.Symbol
        );
    }
}