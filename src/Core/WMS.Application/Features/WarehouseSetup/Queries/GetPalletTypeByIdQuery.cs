using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record PalletTypeDetailDto(
    Guid Id,
    string Name,
    decimal TareWeight,
    decimal Length,
    decimal Width,
    decimal Height,
    bool IsActive);

public record GetPalletTypeByIdQuery(Guid Id) : IRequest<PalletTypeDetailDto?>;

public class GetPalletTypeByIdQueryHandler(IPalletTypeRepository palletTypeRepository)
    : IRequestHandler<GetPalletTypeByIdQuery, PalletTypeDetailDto?>
{
    public async Task<PalletTypeDetailDto?> Handle(GetPalletTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var palletType = await palletTypeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (palletType == null) return null;

        return new PalletTypeDetailDto(
            palletType.Id,
            palletType.Name,
            palletType.TareWeight,
            palletType.Length,
            palletType.Width,
            palletType.Height,
            palletType.IsActive
        );
    }
}