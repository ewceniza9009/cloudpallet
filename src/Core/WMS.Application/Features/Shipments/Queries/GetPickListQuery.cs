using AutoMapper;
using MediatR;
using WMS.Application.Abstractions.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Features.Shipments.Queries;

public record PickItemDto(Guid PickId, string Material, string Sku, string Location, decimal Quantity, string Status);

public record PickListGroupDto
{
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public List<PickItemDto> Items { get; init; } = new();
}

public record GetPickListQuery(Guid UserId) : IRequest<IEnumerable<PickListGroupDto>>;

public class GetPickListQueryHandler(IPickTransactionRepository pickRepository, IMapper mapper)
    : IRequestHandler<GetPickListQuery, IEnumerable<PickListGroupDto>>
{
    public async Task<IEnumerable<PickListGroupDto>> Handle(GetPickListQuery request, CancellationToken cancellationToken)
    {
        var pickTransactions = await pickRepository.GetPendingPicksWithDetailsAsync(request.UserId, cancellationToken);

        var groupedPicks = pickTransactions
            .GroupBy(p => p.AccountId)
            .Select(g => new PickListGroupDto
            {
                AccountId = g.Key,
                AccountName = g.First().Account.Name,
                Items = mapper.Map<List<PickItemDto>>(g.ToList())
            })
            .OrderBy(g => g.AccountName)
            .ToList();

        return groupedPicks;
    }
}