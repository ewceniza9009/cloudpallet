using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Shipments.Queries;

public record ShippableGroupDto
{
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal TotalQuantity { get; init; }
    public List<Guid> PickTransactionIds { get; init; } = new();
}

public record GetShippablePicksQuery(Guid WarehouseId) : IRequest<IEnumerable<ShippableGroupDto>>;

public class GetShippablePicksQueryHandler(IPickTransactionRepository pickRepository)
    : IRequestHandler<GetShippablePicksQuery, IEnumerable<ShippableGroupDto>>
{
    public async Task<IEnumerable<ShippableGroupDto>> Handle(GetShippablePicksQuery request, CancellationToken cancellationToken)
    {
        return await pickRepository.GetConfirmedPicksGroupedByAccountAsync(request.WarehouseId, cancellationToken);
    }
}