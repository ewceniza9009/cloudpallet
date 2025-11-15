using MediatR;
using WMS.Application.Abstractions.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WMS.Application.Features.Inventory.Queries;

public record PalletMovementDto
{
    public string EventType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
}

public record GetPalletHistoryQuery(string PalletBarcode) : IRequest<IEnumerable<PalletMovementDto>>;

public class GetPalletHistoryQueryHandler(IReceivingTransactionRepository receivingRepository)  
    : IRequestHandler<GetPalletHistoryQuery, IEnumerable<PalletMovementDto>>
{
    public async Task<IEnumerable<PalletMovementDto>> Handle(GetPalletHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await receivingRepository.GetPalletHistoryAsync(request.PalletBarcode, cancellationToken);
        return history;
    }
}